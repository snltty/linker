using linker.libs;
using linker.libs.timer;
using System.Collections.Concurrent;
using System.Net;

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
                    foreach (var itemSub in item.CacheSubs)
                    {
                        caches.TryAdd($"{itemSub.Key}", itemSub);
                    }
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
                cache.Network = NetworkHelper.ToNetworkValue(cache.IP, cache.PrefixValue);
                cache.Broadcast = NetworkHelper.ToBroadcastValue(cache.IP, cache.PrefixValue);
                cache.Info = info;

                foreach (var item in caches.Where(c => c.Key.Contains($"{key}-")).Select(c => c.Key).ToList())
                {
                    caches.TryRemove(item, out _);
                }
                cache.CacheSubs = cache.Info.Subs.Select(c =>
                {
                    string _key = $"{key}-{c.Name}";
                    if (caches.TryGetValue(_key, out LeaseCacheInfo _cache) == false)
                    {
                        _cache = new LeaseCacheInfo { Key = _key };
                        caches.TryAdd(_key, _cache);
                    }
                    _cache.IP = NetworkHelper.ToValue(c.IP);
                    _cache.PrefixValue = NetworkHelper.ToPrefixValue(c.PrefixLength);
                    _cache.Network = NetworkHelper.ToNetworkValue(_cache.IP, _cache.PrefixValue);
                    _cache.Broadcast = NetworkHelper.ToBroadcastValue(_cache.IP, _cache.PrefixValue);
                    return _cache;
                }).ToList();

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
                return cache.Info;
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

            LeaseCacheInfo topCache = cache;
            cache.LastTime = DateTime.Now;
            info.Name = cache.Info.Name;

            //如果选择子网
            if (string.IsNullOrWhiteSpace(info.SubName) == false && caches.TryGetValue($"{key}-{info.SubName}", out LeaseCacheInfo _cache))
            {
                _cache.Users = cache.Users;
                cache = _cache;
            }
            info.PrefixLength = NetworkHelper.ToPrefixLength(cache.PrefixValue);

            lock (cache)
            {
                //已有ip
                uint newIPValue = ExistsIp(userId, info.IP, cache);
                if(newIPValue != 0)
                {
                    info.IP = NetworkHelper.ToIP(newIPValue);
                    return info;
                }

                //0.0.0.0 表示动态分配
                if (info.IP.Equals(IPAddress.Any))
                {
                    newIPValue = DynamicIp(userId, cache);
                }
                else
                {
                    newIPValue = StaticIp(userId, info.IP, cache);
                    //静态分配失败
                    if (newIPValue == 0)
                    {
                        newIPValue = DynamicIp(userId, cache);
                    }
                }
                //还是失败了
                if (newIPValue == 0)
                {
                    //更新一下网络号
                    info.IP = NetworkHelper.ToIP(NetworkHelper.ToValue(info.IP) & ~cache.PrefixValue | cache.Network);
                    return info;
                }
                cache.Users.Add(new LeaseCacheUserInfo { Id = userId, IP = newIPValue, LastTime = DateTime.Now });
                leaseServerStore.Update(topCache);
                leaseServerStore.Confirm();

                info.IP = NetworkHelper.ToIP(newIPValue);
                return info;
            }
        }

        /// <summary>
        /// 已存在ip
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private uint ExistsIp(string userId, IPAddress ip, LeaseCacheInfo cache)
        {
            LeaseCacheUserInfo self = cache.Users.FirstOrDefault(c => c.Id == userId);
            if (self != null)
            {
                uint value = NetworkHelper.ToValue(ip);

                bool validate = value == self.IP
                    && cache.InRange(value)
                    && cache.InSub(value) == false;

                if (validate || value == 0)
                {
                    self.LastTime = DateTime.Now;
                    self.IP = self.IP & ~cache.PrefixValue | cache.Network;
                    return self.IP;
                }
                cache.Users.Remove(self);
            }
            return 0;
        }
        /// <summary>
        /// 静态IP
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ip"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private uint StaticIp(string userId, IPAddress ip, LeaseCacheInfo cache)
        {
            uint value = NetworkHelper.ToValue(ip) & ~cache.PrefixValue | cache.Network;

            //不在范围内
            if (cache.InRange(value) == false)
            {
                return 0;
            }
            //被子网占用
            if (cache.InSub(value))
            {
                return 0;
            }
            //已被分配
            if (cache.InLease(userId, value, leaseServerStore.Info.IPDays))
            {
                return 0;
            }
            return value;
        }
        /// <summary>
        /// 动态分配
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        private uint DynamicIp(string userId, LeaseCacheInfo cache)
        {
            //第一个可用IP
            uint firstIPValue = cache.Network;
            //最后一个可用IP
            uint lastIPValue = cache.Broadcast;
            //IP数
            uint length = lastIPValue - cache.Network;

            //子网，排除子网
            IEnumerable<uint> subs = cache.CacheSubs.SelectMany(c => UIntRange(c.Network, c.Broadcast));
            //空闲的IP
            IEnumerable<uint> idleIPs = UIntRange(firstIPValue, length + 1).Except(cache.Users.Select(c => c.IP)).Except(subs);
            //过期的IP
            IEnumerable<uint> expireIPs = cache.Users
                .Where(c => (DateTime.Now - c.LastTime).TotalDays > leaseServerStore.Info.IPDays)
                .OrderBy(c => c.LastTime).Select(c => c.IP).Except(subs);

            uint newIPValue = idleIPs.FirstOrDefault();
            //没找到空闲的，但是有其它超时的，抢一个
            if (newIPValue == 0 && expireIPs.Any())
            {
                newIPValue = expireIPs.FirstOrDefault();
                cache.Users.Remove(cache.Users.FirstOrDefault(c => c.IP == newIPValue));
            }
            return newIPValue;

            IEnumerable<uint> UIntRange(uint start, uint count)
            {
                for (uint i = 0; i < count; i++)
                {
                    yield return start + i;
                }
            }
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
        public byte PrefixLength { get; set; } = 24;
        /// <summary>
        /// 网卡名
        /// </summary>
        public string Name { get; set; } = "linker";

        /// <summary>
        /// 仅传输，不需要任何设置
        /// </summary>
        public string SubName { get; set; } = string.Empty;

        public List<LeaseSubInfo> Subs { get; set; } = [];

        public int Mtu { get; set; } = 1420;

        public int MssFix { get; set; }
    }
    public sealed partial class LeaseSubInfo
    {
        public LeaseSubInfo() { }

        /// <summary>
        /// 网络号
        /// </summary>
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀，掩码长度
        /// </summary>
        public byte PrefixLength { get; set; } = 32;
        /// <summary>
        /// 网络名
        /// </summary>
        public string Name { get; set; } = string.Empty;

    }

    public sealed class LeaseCacheInfo
    {
        public string Id { get; set; }
        public string Key { get; set; }

        /// <summary>
        /// IP
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 前缀，掩码
        /// </summary>
        public uint PrefixValue { get; set; }

        /// <summary>
        /// 网络
        /// </summary>
        public uint Network { get; set; }
        /// <summary>
        /// 广播
        /// </summary>
        public uint Broadcast { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastTime { get; set; } = DateTime.Now;

        public List<LeaseCacheUserInfo> Users { get; set; } = new List<LeaseCacheUserInfo>();
        public List<LeaseCacheInfo> CacheSubs { get; set; } = new List<LeaseCacheInfo>();

        public LeaseInfo Info { get; set; } = new LeaseInfo();

        /// <summary>
        /// 交集
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="prefixLength"></param>
        /// <returns></returns>
        public bool Intersect(IPAddress ip, byte prefixLength)
        {
            uint _ip = NetworkHelper.ToValue(ip);
            uint _prefix = NetworkHelper.ToPrefixValue(prefixLength);
            uint _network = NetworkHelper.ToNetworkValue(_ip, _prefix);
            uint _broadcast = NetworkHelper.ToBroadcastValue(_ip, _prefix);

            return Math.Max(_network, Network) <= Math.Min(_broadcast, Broadcast);
        }
        /// <summary>
        /// 在范围内
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool InRange(uint ip)
        {
            return ip >= Network && ip <= Broadcast;
        }
        /// <summary>
        /// 在子网
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool InSub(uint ip)
        {
            return CacheSubs.Any(c => ip >= c.Network && ip <= c.Broadcast);
        }
        /// <summary>
        /// 已分配
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="ip"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public bool InLease(string userid, uint ip, int days)
        {
            LeaseCacheUserInfo other = Users.FirstOrDefault(c => c.IP == ip && c.Id != userid);
            if (other != null)
            {
                if ((DateTime.Now - other.LastTime).Days <= days)
                {
                    return true;

                }
                Users.Remove(other);
            }
            return false;
        }
    }
    public sealed class LeaseCacheUserInfo
    {
        public string Id { get; set; }
        public uint IP { get; set; }
        public DateTime LastTime { get; set; } = DateTime.Now;
    }
}
