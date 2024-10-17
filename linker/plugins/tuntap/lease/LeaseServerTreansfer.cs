using linker.store;
using LiteDB;
using System.Net;
using linker.libs;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.tuntap.lease
{
    public sealed class LeaseServerTreansfer
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<LeaseCacheInfo> liteCollection;

        private ConcurrentDictionary<string, LeaseCacheInfo> caches { get; set; } = new ConcurrentDictionary<string, LeaseCacheInfo>();

        public LeaseServerTreansfer(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<LeaseCacheInfo>("dhcp");
            foreach (var item in liteCollection.FindAll())
            {
                caches.TryAdd(item.Key, item);
            }
            ClearTask();
        }

        /// <summary>
        /// 添加网络配置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="info"></param>
        public void Add(string key, LeaseInfo info)
        {
            if (info.PrefixLength < 16 || info.PrefixLength >= 32)
            {
                return;
            }

            if (caches.TryGetValue(key, out LeaseCacheInfo cache) == false)
            {
                cache = new LeaseCacheInfo { Key = key };
                cache.Id = ObjectId.NewObjectId();
                liteCollection.Insert(cache);
                caches.TryAdd(key, cache);
            }

            uint oldIP = cache.IP;
            uint oldPrefix = cache.PrefixValue;

            cache.IP = NetworkHelper.IP2Value(info.IP);
            cache.PrefixValue = NetworkHelper.PrefixLength2Value((byte)info.PrefixLength);

            //网络配置有变化，清理分配，让他们重新申请
            if (oldIP != cache.IP || oldPrefix != cache.PrefixValue)
            {
                cache.Users.Clear();
            }

            liteCollection.Update(cache);
            dBfactory.Confirm();
        }
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LeaseInfo Get(string key)
        {
            if (caches.TryGetValue(key, out LeaseCacheInfo cache) == false)
            {
                cache = new LeaseCacheInfo { Key = key };
            }
            return new LeaseInfo { IP = NetworkHelper.Value2IP(cache.IP), PrefixLength = NetworkHelper.PrefixValue2Length(cache.PrefixValue) };
        }


        /// <summary>
        /// 租赁
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="key">配置项</param>
        /// <param name="ip">静态IP</param>
        /// <returns>两种种结果，0.0.0.0 和 ip</returns>
        public IPAddress Lease(string userId, string key, IPAddress ip)
        {
            if (string.IsNullOrWhiteSpace(key) || caches.TryGetValue(key, out LeaseCacheInfo cache) == false)
            {
                return ip;
            }

            lock (cache)
            {
                IPAddress result = IPAddress.Any;

                if (ip.Equals(IPAddress.Any) == false)
                {
                    result = StaticIP(userId, ip, cache);
                }
                if (result.Equals(IPAddress.Any))
                {
                    result = DynamicIP(userId, cache);
                }

                return result;
            }
        }
        private IPAddress StaticIP(string userId, IPAddress ip, LeaseCacheInfo cache)
        {
            uint value = NetworkHelper.IP2Value(ip);
            //不是同一个网络
            if (NetworkHelper.NetworkValue2Value(value, cache.PrefixValue) != NetworkHelper.NetworkValue2Value(cache.IP, cache.PrefixValue))
            {
                return IPAddress.Any;
            }

            LeaseCacheUserInfo self = cache.Users.FirstOrDefault(c => c.Id == userId);
            LeaseCacheUserInfo other = cache.Users.FirstOrDefault(c => c.IP == value && c.Id != userId);

            //这个IP没人用
            if (other == null)
            {
                //我自己有记录，更新IP即可
                if (self != null)
                {
                    self.IP = value;
                    self.LastTime = DateTime.Now;
                }
                else
                {
                    cache.Users.Add(new LeaseCacheUserInfo { Id = userId, IP = value, LastTime = DateTime.Now });
                }
                liteCollection.Update(cache);
                dBfactory.Confirm();
            }
            //有人用
            else
            {
                //用之前申请到的IP
                if (self != null)
                {
                    return NetworkHelper.Value2IP(self.IP);
                }
                //对方没过期，那就不能申占用了
                if ((DateTime.Now - other.LastTime).Days <= 7)
                {
                    return IPAddress.Any;
                }

                other.IP = value;
                other.LastTime = DateTime.Now;
                other.Id = userId;
                liteCollection.Update(cache);
                dBfactory.Confirm();
            }

            return ip;
        }
        private IPAddress DynamicIP(string userId, LeaseCacheInfo cache)
        {
            //网络号
            uint network = NetworkHelper.NetworkValue2Value(cache.IP, cache.PrefixValue);
            //广播
            uint broadcast = NetworkHelper.BroadcastValue2Value(cache.IP, cache.PrefixValue);
            //第一个可用IP
            uint firstValue = network + 1;
            //最后一个可用IP
            uint lastValue = broadcast - 1;
            //IP数
            uint length = lastValue - network;

            IEnumerable<int> ips = Enumerable.Range((int)firstValue, (int)length + 1).Except(cache.Users.Select(c => (int)c.IP));
            if (ips.Any() == false)
            {
                return IPAddress.Any;
            }

            uint ipValue = (uint)ips.FirstOrDefault();
            cache.Users.Add(new LeaseCacheUserInfo { Id = userId, IP = ipValue, LastTime = DateTime.Now });
            liteCollection.Update(cache);
            dBfactory.Confirm();

            return NetworkHelper.Value2IP(ipValue);
        }


        /// <summary>
        /// 租期
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="key"></param>
        public void LeaseExp(string userid, string key)
        {
            if (caches.TryGetValue(key, out LeaseCacheInfo cache))
            {
                cache.LastTime = DateTime.Now;
                LeaseCacheUserInfo user = cache.Users.FirstOrDefault(c => c.Id == userid);
                if (user != null)
                {
                    user.LastTime = DateTime.Now;
                }
                liteCollection.Update(cache);
                dBfactory.Confirm();
            }
        }

        private void ClearTask()
        {
            TimerHelper.SetInterval(() =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"start cleaning up dhcp that have exceeded the 30-day timeout period");
                }

                try
                {
                    DateTime now = DateTime.Now;

                    var items = caches.Values.Where(c => (now - c.LastTime).TotalDays > 30).ToList();
                    if (items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            caches.TryRemove(item.Key, out _);
                            liteCollection.Delete(item.Id);
                        }
                        dBfactory.Confirm();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Debug($"cleaning up dhcp error {ex}");
                }

                return true;
            }, 5 * 60 * 1000);
        }
    }

    [MemoryPackable]
    public sealed partial class LeaseInfo
    {
        /// <summary>
        /// 网络号
        /// </summary>
        [MemoryPackAllowSerialize]
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀，掩码长度
        /// </summary>
        public int PrefixLength { get; set; } = 32;
    }

    public sealed class LeaseCacheInfo
    {
        public ObjectId Id { get; set; }

        public string Key { get; set; }

        /// <summary>
        /// 网络号
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 前缀，掩码
        /// </summary>
        public uint PrefixValue { get; set; }
        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastTime { get; set; } = DateTime.Now;

        public List<LeaseCacheUserInfo> Users { get; set; } = new List<LeaseCacheUserInfo>();
    }

    public sealed class LeaseCacheUserInfo
    {
        public string Id { get; set; }
        public uint IP { get; set; }
        public DateTime LastTime { get; set; } = DateTime.Now;
    }
}
