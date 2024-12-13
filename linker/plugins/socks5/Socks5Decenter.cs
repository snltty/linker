using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using linker.plugins.route;
using linker.plugins.socks5.config;
using linker.plugins.tunnel;
using linker.plugins.tuntap;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;

namespace linker.plugins.socks5
{
    public sealed class Socks5Decenter : IDecenter
    {
        public string Name => "socks5";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        private readonly ConcurrentDictionary<string, Socks5Info> socks5Infos = new ConcurrentDictionary<string, Socks5Info>();
        public ConcurrentDictionary<string, Socks5Info> Infos => socks5Infos;

        private readonly ClientSignInState clientSignInState;
        private readonly TunnelProxy tunnelProxy;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly Socks5ConfigTransfer socks5ConfigTransfer;
        private readonly RouteExcludeIPTransfer routeExcludeIPTransfer;

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public Socks5Decenter( ClientSignInState clientSignInState, TunnelProxy tunnelProxy, ClientConfigTransfer clientConfigTransfer, Socks5ConfigTransfer socks5ConfigTransfer, RouteExcludeIPTransfer routeExcludeIPTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.tunnelProxy = tunnelProxy;
            this.clientConfigTransfer = clientConfigTransfer;
            this.socks5ConfigTransfer = socks5ConfigTransfer;
            this.routeExcludeIPTransfer = routeExcludeIPTransfer;

            clientSignInState.NetworkEnabledHandle += (times) => Refresh();
            tunnelProxy.RefreshConfig += Refresh;
            socks5ConfigTransfer.OnChanged += Refresh;


        }

        /// <summary>
        /// 刷新信息，把自己的配置发给别人，顺便把别人的信息带回来
        /// </summary>
        public void Refresh()
        {
            SyncVersion.Add();
        }
        public Memory<byte> GetData()
        {
            Socks5Info info = new Socks5Info
            {
                Lans = socks5ConfigTransfer.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false; return c; }).ToList(),
                MachineId = clientConfigTransfer.Id,
                Status = tunnelProxy.Running ? Socks5Status.Running : Socks5Status.Normal,
                Port = socks5ConfigTransfer.Port,
                SetupError = tunnelProxy.Error
            };
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            DataVersion.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            Socks5Info info = MemoryPackSerializer.Deserialize<Socks5Info>(data.Span);
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();
                try
                {
                    socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                    DataVersion.Add();
                    AddRoute();
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                slim.Release();
            });
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<Socks5Info> list = data.Select(c => MemoryPackSerializer.Deserialize<Socks5Info>(c.Span)).ToList();
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();

                try
                {
                    foreach (var item in list)
                    {
                        socks5Infos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                        item.LastTicks.Update();
                    }
                    var removes = socks5Infos.Keys.Except(list.Select(c => c.MachineId)).Where(c => c != clientConfigTransfer.Id).ToList();
                    foreach (var item in removes)
                    {
                        if (socks5Infos.TryGetValue(item, out Socks5Info socks5Info))
                        {
                            socks5Info.Status = Socks5Status.Normal;
                            socks5Info.LastTicks.Clear();
                        }
                    }
                    DataVersion.Add();
                    AddRoute();
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                finally
                {
                    slim.Release();
                }

            });
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<Socks5LanIPAddressList> ipsList = ParseIPs(socks5Infos.Values.ToList());
            Socks5LanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();

            tunnelProxy.SetIPs(ips);
        }

        private List<Socks5LanIPAddressList> ParseIPs(List<Socks5Info> infos)
        {
            //排除的IP，
            uint[] excludeIps = routeExcludeIPTransfer.Get().Concat(socks5ConfigTransfer.Lans.Select(c => c.IP))
                .Select(NetworkHelper.IP2Value)
                .ToArray();

            HashSet<uint> hashSet = new HashSet<uint>();

            return infos
                .Where(c => c.MachineId != clientConfigTransfer.Id)
                .OrderByDescending(c => c.Status)
                .OrderByDescending(c => c.LastTicks.Value)

                .Select(c =>
                {
                    var lans = c.Lans.Where(c => c.Disabled == false && c.IP.Equals(IPAddress.Any) == false).Where(c =>
                    {
                        uint ipInt = NetworkHelper.IP2Value(c.IP);
                        uint maskValue = NetworkHelper.PrefixLength2Value(c.PrefixLength);
                        uint network = ipInt & maskValue;
                        c.Exists = excludeIps.Any(d => (d & maskValue) == network) || hashSet.Contains(network);
                        hashSet.Add(network);
                        return c.Exists == false;
                    });

                    return new Socks5LanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(lans.ToList(), c.MachineId)
                        .Where(c => excludeIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<Socks5LanIPAddress> ParseIPs(List<Socks5LanInfo> lans, string machineid)
        {
            return lans.Where(c => c.IP.Equals(IPAddress.Any) == false && c != null).Select((c, index) =>
            {
                return ParseIPAddress(c.IP, c.PrefixLength, machineid);

            }).ToList();
        }
        private Socks5LanIPAddress ParseIPAddress(IPAddress ip, byte prefixLength, string machineid)
        {
            uint ipInt = NetworkHelper.IP2Value(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.PrefixLength2Value(prefixLength);
            return new Socks5LanIPAddress
            {
                IPAddress = ipInt,
                PrefixLength = prefixLength,
                MaskValue = maskValue,
                NetWork = ipInt & maskValue,
                Broadcast = ipInt | ~maskValue,
                OriginIPAddress = ip,
                MachineId = machineid
            };
        }

    }
}
