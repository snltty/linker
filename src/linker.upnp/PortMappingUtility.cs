using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.upnp
{
    public static class PortMappingUtility
    {
        /// <summary>
        /// 当发现设备
        /// </summary>
        public static event Action<IPortMappingDevice> OnDeviceFound
        {
            add => DeviceFound += value;
            remove => DeviceFound -= value;
        }
        private static Action<IPortMappingDevice> DeviceFound = (device) => { };

        /// <summary>
        /// 当发生变化，发现设备，添加删除映射什么的
        /// </summary>
        public static event Action OnChange
        {
            add => Change += value;
            remove => Change -= value;
        }
        private static Action Change = () => { };

        /// <summary>
        /// 设备数
        /// </summary>
        public static int DeviceCount => devices.Count;
        /// <summary>
        /// 映射数
        /// </summary>
        public static int MappingCount => mappings.Count;
        /// <summary>
        /// 本地数
        /// </summary>
        public static int LocalMappingCount => localMappings.Where(c => c.Value.Deleted == false).Count();
        /// <summary>
        /// 外网数量
        /// </summary>
        public static int WanCount => devices.Values.Count(c => IsPrivateIP(c.WanIp) == false);


        private static readonly IPortMappingService[] services = [new PortMappingUpnpService(), new PortMappingPmpService()];
        private static readonly ConcurrentDictionary<(IPAddress gateway, DeviceType deviceType), IPortMappingDevice> devices = new();
        private static readonly ConcurrentDictionary<(int publicPort, ProtocolType protocolType), MappingCacheInfo> localMappings = new();
        private static List<PortMappingInfo> mappings = [];

        private static CancellationTokenSource cts;
        private static CancellationTokenSource _cts;
        private static long ticks = Environment.TickCount64;


        /// <summary>
        /// 开始发现设备
        /// </summary>
        /// <param name="deviceType"></param>
        public static void StartDiscovery(DeviceType deviceType = DeviceType.All)
        {
            if (cts != null && cts.Token.IsCancellationRequested == false)
            {
                return;
            }
            cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (cts.IsCancellationRequested == false)
                {
                    try
                    {
                        for (int i = 0; i < services.Length; i++)
                        {
                            await services[i].Discovery(cts.Token).ConfigureAwait(false);
                        }

                        RefreshDevice();

                        await Add().ConfigureAwait(false);
                        await Delete().ConfigureAwait(false);
                        await RefreshMappings().ConfigureAwait(false);
                        await Delay().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }
                }
               
            });
        }
        private static void RefreshDevice()
        {
            foreach (var device in services.Select(c => c.GetDevices()).ToList().SelectMany(c => c))
            {
                if (devices.TryGetValue((device.GatewayIp, device.Type), out IPortMappingDevice _device) == false)
                {
                    devices.TryAdd((device.GatewayIp, device.Type), device);
                    DeviceFound?.Invoke(device);
                }
            }
            Change?.Invoke();
        }
        private static async Task RefreshMappings()
        {
            mappings = (await Task.WhenAll(services.Select(c => c.Get()).ToList()).ConfigureAwait(false))
                       .SelectMany(c => c)
                       .Where(c => localMappings.TryGetValue((c.PublicPort, c.ProtocolType), out MappingCacheInfo cache) == false || cache.Deleted == false)
                       .ToList();
            Change?.Invoke();
        }
        private static async Task Delay()
        {
            _cts = new CancellationTokenSource();
            try
            {
                int delay = Math.Max((int)Math.Min(30000, Environment.TickCount64 - ticks), 3000);
                await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
            }
        }
        private static void RefreshDelay()
        {
            ticks = Environment.TickCount64;
            _cts?.Cancel();
        }

        /// <summary>
        /// 停止发现设备
        /// </summary>
        public static void StopDiscovery()
        {
            cts?.Cancel();
            _cts?.Cancel();
        }

        /// <summary>
        /// 获取所有已发现设备
        /// </summary>
        /// <returns></returns>
        public static List<IPortMappingDevice> GetDevices()
        {
            RefreshDelay();
            return services.Select(c => c.GetDevices()).ToList().SelectMany(c => c).ToList();
        }

        /// <summary>
        /// 获取所有已发现设备的所有映射信息
        /// </summary>
        /// <returns></returns>
        public static List<PortMappingInfo> Get()
        {
            RefreshDelay();
            return mappings
                .Where(c => localMappings.TryGetValue((c.PublicPort, c.ProtocolType), out MappingCacheInfo cache) == false || cache.Deleted == false)
                .ToList();
        }
        /// <summary>
        /// 获取本地添加的映射信息（不包含已删除的）
        /// </summary>
        /// <returns></returns>
        public static List<PortMappingInfo> GetLocal()
        {
            RefreshDelay();
            return localMappings.Values.Where(c => c.Deleted == false).Select(c => c.Info).ToList();
        }

        /// <summary>
        /// 添加一条映射
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static async Task Add(PortMappingInfo mapping)
        {
            MappingCacheInfo cache = new MappingCacheInfo { Info = mapping };
            localMappings.AddOrUpdate((mapping.PublicPort, mapping.ProtocolType), cache, (k, v) => cache);
            await AddInternal(cache.Info).ConfigureAwait(false);
            RefreshDelay();
        }
        private static async Task Add()
        {
            foreach (var cache in localMappings.Values.Where(c => c.Deleted == false))
            {
                await AddInternal(cache.Info).ConfigureAwait(false);
            }
        }
        private static async Task AddInternal(PortMappingInfo mapping)
        {
            for (int i = 0; i < services.Length; i++)
            {
                if ((services[i].Type & mapping.DeviceType) == services[i].Type && services[i].GetDevices().Count > 0)
                {
                    await services[i].Add(mapping).ConfigureAwait(false);
                    PortMappingInfo _mapping = await services[i].Get(mapping.PublicPort, mapping.ProtocolType).ConfigureAwait(false);
                    if (_mapping != null && _mapping.LeaseDuration > 0 && _mapping.Description == mapping.Description)
                    {
                        break;
                    }
                    if (_mapping.Description != mapping.Description)
                    {
                        await services[i].Delete(mapping.PublicPort, mapping.ProtocolType).ConfigureAwait(false);
                    }
                }
            }
            Change?.Invoke();
        }

        /// <summary>
        /// 删除一条映射
        /// </summary>
        /// <param name="publicPort"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static async Task Delete(int publicPort, ProtocolType protocol, bool force = false)
        {
            if (localMappings.TryGetValue((publicPort, protocol), out MappingCacheInfo cache))
            {
                cache.Deleted = true;
                if (cache.Info.Deletable == false && force)
                {
                    cache.Info.Deletable = true;
                }
            }
            else
            {
                cache = new MappingCacheInfo { Deleted = true, Info = new PortMappingInfo { PublicPort = publicPort, ProtocolType = protocol } };
                localMappings.TryAdd((publicPort, protocol), cache);
            }

            for (int i = 0; i < services.Length; i++)
            {
                if ((services[i].Type & cache.Info.DeviceType) == services[i].Type)
                {
                    await services[i].Delete(publicPort, protocol).ConfigureAwait(false);
                }
            }

            RefreshDelay();
            Change?.Invoke();
        }
        private static async Task Delete()
        {
            foreach (var cache in localMappings.Values.Where(c => c.Deleted))
            {
                for (int i = 0; i < services.Length; i++)
                {
                    if ((services[i].Type & cache.Info.DeviceType) == services[i].Type)
                    {
                        await services[i].Delete(cache.Info.PublicPort, cache.Info.ProtocolType).ConfigureAwait(false);
                    }
                }
            }
            foreach (var kv in localMappings.Where(c => c.Value.Deleted).ToList())
            {
                if (kv.Value.Info.Deletable)
                    localMappings.TryRemove(kv.Key, out _);
                else
                {
                    kv.Value.Deleted = false;
                }
            }
        }

        sealed class MappingCacheInfo
        {
            public PortMappingInfo Info { get; set; }
            public bool Deleted { get; set; }
        }


        private static readonly HashSet<IPNetwork> privateNetworks = new HashSet<IPNetwork>
        {
            // IPv4 私有网络
            IPNetwork.Parse("127.0.0.0/8"),    // 回环
            IPNetwork.Parse("10.0.0.0/8"),      // 私有A类
            IPNetwork.Parse("172.16.0.0/12"),   // 私有B类
            IPNetwork.Parse("192.168.0.0/16"),  // 私有C类
            IPNetwork.Parse("169.254.0.0/16"),  // 链路本地
            IPNetwork.Parse("100.64.0.0/10"),   // CGNAT (可选)
        
            // IPv6 私有网络
            IPNetwork.Parse("fc00::/7"),         // ULA
            IPNetwork.Parse("fe80::/10"),        // 链路本地
            IPNetwork.Parse("::1/128"),          // 回环
        };
        private static bool IsPrivateIP(IPAddress ip)
        {
            return privateNetworks.Any(network => network.Contains(ip));
        }
    }
}
