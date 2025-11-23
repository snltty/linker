using System.Net;
using linker.libs;
using System.Collections.Concurrent;
using linker.libs.timer;

namespace linker.messenger.tuntap.lease
{
    public sealed class LeaseServerTreansfer
    {

        private ConcurrentDictionary<string, LeaseCacheInfo> caches { get; set; } = new ConcurrentDictionary<string, LeaseCacheInfo>();

        private readonly ILeaseServerStore leaseServerStore;
        public LeaseServerTreansfer(ILeaseServerStore leaseServerStore)
        {
            this.leaseServerStore = leaseServerStore;
            try
            {
                foreach (var item in leaseServerStore.Get())
                {
                    caches.TryAdd(item.Key, item);
                }
            }
            catch (Exception)
            {
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
                    leaseServerStore.Remove(remove.Id);
                    leaseServerStore.Confirm();
                }
            }
            else
            {
                if (caches.TryGetValue(key, out LeaseCacheInfo cache) == false)
                {
                    cache = new LeaseCacheInfo { Key = key };
                    leaseServerStore.Add(cache);
                    caches.TryAdd(key, cache);
                }

                uint oldIP = cache.IP;
                uint oldPrefix = cache.PrefixValue;

                cache.IP = NetworkHelper.ToValue(info.IP);
                cache.PrefixValue = NetworkHelper.ToPrefixValue(info.PrefixLength);
                cache.Name = info.Name;

                //网络配置有变化，清理分配，让他们重新申请
                if (oldIP != cache.IP || oldPrefix != cache.PrefixValue)
                {
                    cache.Users.Clear();
                }
                leaseServerStore.Update(cache);
                leaseServerStore.Confirm();
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
                return new LeaseInfo { IP = NetworkHelper.ToIP(cache.IP), PrefixLength = NetworkHelper.ToPrefixLength(cache.PrefixValue), Name = cache.Name };
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
            info.PrefixLength = NetworkHelper.ToPrefixLength(cache.PrefixValue);
            info.Name = cache.Name;


            LeaseCacheUserInfo self = cache.Users.FirstOrDefault(c => c.Id == userId);
            //已有的
            if (self != null)
            {
                if (NetworkHelper.ToValue(info.IP) == self.IP || info.IP.Equals(IPAddress.Any))
                {
                    self.LastTime = DateTime.Now;
                    uint networkValue = NetworkHelper.ToNetworkValue(cache.IP, cache.PrefixValue);
                    info.IP = NetworkHelper.ToIP(self.IP & ~cache.PrefixValue | networkValue);
                    self.IP = NetworkHelper.ToValue(info.IP);
                    return info;
                }
                cache.Users.Remove(self);
            }

            lock (cache)
            {
                uint newIPValue = info.IP.Equals(IPAddress.Any) ? DynamicIP(userId, cache) : StaticIP(userId, info.IP, cache);
                //分配失败，怎么来的怎么回去
                if (newIPValue == 0)
                {
                    //万一网络号已经不一样了，更新一下
                    uint value = NetworkHelper.ToValue(info.IP);
                    uint networkValue = NetworkHelper.ToNetworkValue(cache.IP, cache.PrefixValue);
                    info.IP = NetworkHelper.ToIP(value & ~cache.PrefixValue | networkValue);
                    return info;
                }

                cache.Users.Add(new LeaseCacheUserInfo { Id = userId, IP = newIPValue, LastTime = DateTime.Now });
                leaseServerStore.Update(cache);
                leaseServerStore.Confirm();

                info.IP = NetworkHelper.ToIP(newIPValue);
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
            uint value = NetworkHelper.ToValue(ip);
            //网络号
            uint networkValue = NetworkHelper.ToNetworkValue(cache.IP, cache.PrefixValue);
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
            uint networkValue = NetworkHelper.ToNetworkValue(cache.IP, cache.PrefixValue);
            //广播
            uint broadcastValue = NetworkHelper.ToBroadcastValue(cache.IP, cache.PrefixValue);
            //第一个可用IP，一般第一个可用IP作为网关，所以+2
            uint firstIPValue = networkValue + 1 + 1;
            //最后一个可用IP，最后一个IP是广播地址，所以-1
            uint lastIPValue = broadcastValue - 1;
            //IP数
            uint length = lastIPValue - networkValue;

            //空闲的IP
            IEnumerable<int> idleIPs = Enumerable.Range((int)firstIPValue, (int)length + 1).Except(cache.Users.Select(c => (int)c.IP));
            //过期的IP
            IEnumerable<int> expireIPs = cache.Users
                .Where(c => (DateTime.Now - c.LastTime).TotalDays > leaseServerStore.Info.IPDays)
                .OrderBy(c => c.LastTime).Select(c => (int)c.IP);

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
                leaseServerStore.Update(cache);
                leaseServerStore.Confirm();
            }
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"start cleaning up dhcp that have exceeded the 30-day timeout period");
                }

                try
                {
                    DateTime now = DateTime.Now;

                    var items = caches.Values.Where(c => (now - c.LastTime).TotalDays > leaseServerStore.Info.NetworkDays).ToList();
                    if (items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            caches.TryRemove(item.Key, out _);
                            leaseServerStore.Remove(item.Id);
                        }
                        leaseServerStore.Confirm();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Debug($"cleaning up dhcp error {ex}");
                }

            }, 5 * 60 * 1000);
        }
    }

    public sealed partial class LeaseInfo
    {
        public LeaseInfo() { }
        /// <summary>
        /// 网络号
        /// </summary>
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀，掩码长度
        /// </summary>
        public byte PrefixLength { get; set; } = 32;
        /// <summary>
        /// 网卡名
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    public sealed class LeaseCacheInfo
    {
        public string Id { get; set; }

        public string Key { get; set; }
        public string Name { get; set; } = string.Empty;

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
