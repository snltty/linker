using linker.libs;
using linker.libs.extends;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.tun;
using linker.tunnel.connection;
using System.Net;

namespace linker.messenger.tuntap
{
    public sealed class TuntapAdapter : ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
        private List<LinkerTunDeviceForwardItem> forwardItems = new List<LinkerTunDeviceForwardItem>();
        private LinkerTunDeviceRouteItem[] routeItems = new LinkerTunDeviceRouteItem[0];

        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapDecenter tuntapDecenter;
        private readonly TuntapProxy tuntapProxy;
        private readonly ISignInClientStore signInClientStore;
        private readonly ExRouteTransfer exRouteTransfer;

        public TuntapAdapter(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, TuntapDecenter tuntapDecenter, TuntapProxy tuntapProxy,
            SignInClientState signInClientState, ISignInClientStore signInClientStore, ExRouteTransfer exRouteTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapDecenter = tuntapDecenter;
            this.tuntapProxy = tuntapProxy;
            this.signInClientStore = signInClientStore;
            this.exRouteTransfer = exRouteTransfer;

            //与服务器连接，刷新一下IP
            signInClientState.NetworkEnabledHandle += (times) => tuntapConfigTransfer.RefreshIP();

            //初始化网卡
            tuntapTransfer.Init(tuntapConfigTransfer.DeviceName, this);
            //网卡状态发生变化，同步一下信息
            tuntapTransfer.OnSetupBefore += () => { tuntapDecenter.Refresh(); };
            tuntapTransfer.OnSetupAfter += () => { tuntapDecenter.Refresh(); };
            tuntapTransfer.OnSetupSuccess += () =>
            {
                AddForward();
                tuntapConfigTransfer.SetRunning(true);
            };
            tuntapTransfer.OnShutdownBefore += () => { tuntapDecenter.Refresh(); };
            tuntapTransfer.OnShutdownAfter += () => { tuntapDecenter.Refresh(); };
            tuntapTransfer.OnShutdownSuccess += () => { DeleteForward(); tuntapConfigTransfer.SetRunning(false); };

            //配置有更新，去同步一下
            tuntapConfigTransfer.OnUpdate += () =>
            {
                _ = CheckDevice();
                tuntapDecenter.Refresh();
            };

            //收到新的信息，添加一下路由
            tuntapDecenter.OnChangeAfter += AddRoute;
            tuntapDecenter.OnReset += tuntapProxy.ClearIPs;
            tuntapDecenter.HandleCurrentInfo = GetCurrentInfo;

            //隧道回调
            tuntapProxy.Callback = this;

            CheckDeviceTask();
        }
        private TuntapInfo GetCurrentInfo()
        {
            return new TuntapInfo
            {
                IP = tuntapConfigTransfer.Info.IP,
                Lans = tuntapConfigTransfer.Info.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false; return c; }).ToList(),
                PrefixLength = tuntapConfigTransfer.Info.PrefixLength,
                MachineId = signInClientStore.Id,
                Status = tuntapTransfer.Status,
                SetupError = tuntapTransfer.SetupError,
                NatError = tuntapTransfer.NatError,
                SystemInfo = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}",

                Forwards = tuntapConfigTransfer.Info.Forwards,
                Switch = tuntapConfigTransfer.Info.Switch
            };
        }

        private ulong configVersion = 0;
        private OperatingManager checking = new OperatingManager();
        private void CheckDeviceTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                await CheckDevice();
                return true;
            }, () => 30000);
        }
        private async Task CheckDevice()
        {
            if (checking.StartOperation() == false) return;

            bool version = tuntapConfigTransfer.Version.Eq(configVersion, out ulong _version);
            bool available = await tuntapTransfer.CheckAvailable();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"tuntap device check, version eq:{version},available:{available},running:{tuntapConfigTransfer.Running},status:{tuntapTransfer.Status}");
            }
            bool restart = (version == false || available == false) && tuntapConfigTransfer.Running && tuntapTransfer.Status != TuntapStatus.Operating;
            if (restart)
            {
                LoggerHelper.Instance.Warning($"tuntap config version changed, restarting device");
                configVersion = _version;
                await RetstartDevice();
            }
            checking.StopOperation();
        }

        public async Task Callback(LinkerTunDevicPacket packet)
        {
            if (packet.IPV4Broadcast || packet.IPV6Multicast)
            {
                if ((tuntapConfigTransfer.Switch & TuntapSwitch.Multicast) == TuntapSwitch.Multicast)
                {
                    return;
                }
            }
            await tuntapProxy.InputPacket(packet);
        }

        public async ValueTask Close(ITunnelConnection connection)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask;
        }
        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
            tuntapTransfer.Write(buffer);
            await ValueTask.CompletedTask;
        }
        public async ValueTask NotFound(uint ip)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask;
        }

        /// <summary>
        /// 重启网卡
        /// </summary>
        /// <returns></returns>
        public async Task RetstartDevice()
        {
            tuntapTransfer.Shutdown();
            await tuntapConfigTransfer.RefreshIPASync();
            tuntapTransfer.Setup(tuntapConfigTransfer.Info.IP, tuntapConfigTransfer.Info.PrefixLength);
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public void StopDevice()
        {
            tuntapTransfer.Shutdown();
        }

        // <summary>
        /// 添加端口转发
        /// </summary>
        private void AddForward()
        {
            var temp = ParseForwardItems();
            var removes = forwardItems.Except(temp, new LinkerTunDeviceForwardItemComparer());
            if (removes.Any())
            {
                tuntapTransfer.RemoveForward(removes.ToList());
            }
            forwardItems = temp;
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"add tuntap forward {forwardItems.ToJson()}");
            tuntapTransfer.AddForward(forwardItems);
        }
        /// <summary>
        /// 删除端口转发
        /// </summary>
        public void DeleteForward()
        {
            tuntapTransfer.RemoveForward(forwardItems);
        }
        private List<LinkerTunDeviceForwardItem> ParseForwardItems()
        {
            return tuntapConfigTransfer.Info.Forwards.Select(c => new LinkerTunDeviceForwardItem { ListenAddr = c.ListenAddr, ListenPort = c.ListenPort, ConnectAddr = c.ConnectAddr, ConnectPort = c.ConnectPort }).ToList();
        }

        /// <summary>
        /// 删除路由
        /// </summary>
        private void DelRoute()
        {
            if (routeItems != null)
                tuntapTransfer.DelRoute(routeItems);
        }
        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapDecenter.Infos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var _routeItems = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.PrefixLength }).ToArray();

            var removeItems = routeItems.Except(_routeItems, new LinkerTunDeviceRouteItemComparer()).ToArray();
            if (removeItems.Length > 0)
                tuntapTransfer.DelRoute(removeItems);

            tuntapTransfer.AddRoute(_routeItems, tuntapConfigTransfer.Info.IP);

            tuntapProxy.SetIPs(ips);
            foreach (var item in tuntapDecenter.Infos.Values)
            {
                tuntapProxy.SetIP(item.MachineId, NetworkHelper.IP2Value(item.IP));
            }
            foreach (var item in tuntapDecenter.Infos.Values.Where(c => c.IP.Equals(IPAddress.Any)))
            {
                tuntapProxy.RemoveIP(item.MachineId);
            }

            routeItems = _routeItems;
        }

        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            //排除的IP，
            uint[] excludeIps = exRouteTransfer.Get().Select(NetworkHelper.IP2Value).Distinct().ToArray();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"tuntap route ex ips : {string.Join(",", excludeIps.Select(c => NetworkHelper.Value2IP(c)).ToList())}");

            HashSet<uint> hashSet = new HashSet<uint>();

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)
                .OrderBy(c => c.IP, new IPAddressComparer())
                 .OrderByDescending(c => c.Status)
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

                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(lans.ToList(), c.MachineId)
                        .Where(c => excludeIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<TuntapVeaLanIPAddress> ParseIPs(List<TuntapLanInfo> lans, string machineid)
        {
            return lans.Where(c => c.IP.Equals(IPAddress.Any) == false && c != null).Select((c, index) =>
            {
                return ParseIPAddress(c.IP, c.PrefixLength, machineid);

            }).ToList();
        }
        private TuntapVeaLanIPAddress ParseIPAddress(IPAddress ip, byte prefixLength, string machineid)
        {
            uint ipInt = NetworkHelper.IP2Value(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.PrefixLength2Value(prefixLength);
            return new TuntapVeaLanIPAddress
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


        sealed class IPAddressComparer : IComparer<IPAddress>
        {
            public int Compare(IPAddress x, IPAddress y)
            {
                return (int)(NetworkHelper.IP2Value(x) - NetworkHelper.IP2Value(y));
            }
        }
        sealed class LinkerTunDeviceRouteItemComparer : IEqualityComparer<LinkerTunDeviceRouteItem>
        {
            public bool Equals(LinkerTunDeviceRouteItem x, LinkerTunDeviceRouteItem y)
            {
                return x.Address.Equals(y.Address) && x.PrefixLength == y.PrefixLength;
            }
            public int GetHashCode(LinkerTunDeviceRouteItem obj)
            {
                return obj.Address.GetHashCode() ^ obj.PrefixLength;
            }
        }
    }
}
