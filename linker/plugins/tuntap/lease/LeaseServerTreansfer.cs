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
        public void AddNetwork(string key, LeaseInfo info)
        {
            if (info.PrefixLength < 16 || info.PrefixLength >= 32)
            {
                return;
            }
            if (info.IP.Equals(IPAddress.Any))
            {
                if (caches.TryRemove(key, out LeaseCacheInfo remove))
                {
                    liteCollection.Delete(remove.Id);
                    dBfactory.Confirm();
                }
            }
            else
            {
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
                cache.PrefixValue = NetworkHelper.PrefixLength2Value(info.PrefixLength);

                //网络配置有变化，清理分配，让他们重新申请
                if (oldIP != cache.IP || oldPrefix != cache.PrefixValue)
                {
                    cache.Users.Clear();
                }
                liteCollection.Update(cache);
                dBfactory.Confirm();
            }
        }
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LeaseInfo GetNetwork(string key)
        {
            if (caches.TryGetValue(key, out LeaseCacheInfo cache))
            {
                return new LeaseInfo { IP = NetworkHelper.Value2IP(cache.IP), PrefixLength = NetworkHelper.PrefixValue2Length(cache.PrefixValue) };
            }
            return new LeaseInfo { IP = IPAddress.Any, PrefixLength = 24 };
        }


        /// <summary>
        /// 租赁
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="key"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public LeaseInfo LeaseIP(string userId, string key, LeaseInfo info)
        {
            //未设置网络，怎么来的怎么回去
            if (string.IsNullOrWhiteSpace(key) || caches.TryGetValue(key, out LeaseCacheInfo cache) == false)
            {
                return info;
            }
            cache.LastTime = DateTime.Now;
            info.PrefixLength = NetworkHelper.PrefixValue2Length(cache.PrefixValue);

            LeaseCacheUserInfo self = cache.Users.FirstOrDefault(c => c.Id == userId);
            if (self != null)
            {
                info.IP = NetworkHelper.Value2IP(self.IP);
                return info;
            }

            lock (cache)
            {
                uint newIPValue = info.IP.Equals(IPAddress.Any) ? DynamicIP(userId, cache) : StaticIP(userId, info.IP, cache);
                //分配失败，怎么来的怎么回去
                if (newIPValue == 0)
                {
                    return info;
                }

                cache.Users.Add(new LeaseCacheUserInfo { Id = userId, IP = newIPValue, LastTime = DateTime.Now });
                liteCollection.Update(cache);
                dBfactory.Confirm();

                info.IP = NetworkHelper.Value2IP(newIPValue);
                return info;
            }
        }
        /// <summary>
        /// 静态IP
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private uint StaticIP(string userId, IPAddress ip, LeaseCacheInfo cache)
        {
            uint value = NetworkHelper.IP2Value(ip);
            //网络号
            uint networkValue = NetworkHelper.NetworkValue2Value(cache.IP, cache.PrefixValue);
            //新的IP
            uint newIPValue = value & ~cache.PrefixValue | networkValue;

            LeaseCacheUserInfo other = cache.Users.FirstOrDefault(c => c.IP == newIPValue && c.Id != userId);
            //这个IP有人用
            if (other != null)
            {
                //超时了，删掉
                if ((DateTime.Now - other.LastTime).Days > 7)
                {
                    cache.Users.Remove(other);
                }
                else
                {
                    newIPValue = 0;
                }
            }

            return newIPValue;
        }
        /// <summary>
        /// 动态分配
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private uint DynamicIP(string userId, LeaseCacheInfo cache)
        {
            //网络号
            uint networkValue = NetworkHelper.NetworkValue2Value(cache.IP, cache.PrefixValue);
            //广播
            uint broadcastValue = NetworkHelper.BroadcastValue2Value(cache.IP, cache.PrefixValue);
            //第一个可用IP，一般第一个可用IP作为网关，所以+2
            uint firstIPValue = networkValue + 1 + 1;
            //最后一个可用IP，最后一个IP是广播地址，所以-1
            uint lastIPValue = broadcastValue - 1;
            //IP数
            uint length = lastIPValue - networkValue;

            //空闲的IP
            IEnumerable<int> idleIPs = Enumerable.Range((int)firstIPValue, (int)length + 1).Except(cache.Users.Select(c => (int)c.IP));
            //过期的IP
            IEnumerable<int> expireIPs = cache.Users.Where(c => (DateTime.Now - c.LastTime).TotalDays > 7).Select(c => (int)c.IP);

            uint newIPValue = (uint)idleIPs.FirstOrDefault();
            //没找到空闲的，但是有其它超时的，抢一个
            if (newIPValue == 0 && expireIPs.Any())
            {
                newIPValue = (uint)expireIPs.FirstOrDefault();
                cache.Users.Remove(cache.Users.FirstOrDefault(c => c.IP == newIPValue));
            }
            return newIPValue;
        }


        /// <summary>
        /// 延长租期
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
        public byte PrefixLength { get; set; } = 32;
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
