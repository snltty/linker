using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.messenger.exroute;
using linker.tun;
using System.Net;
using linker.libs.extends;

namespace linker.messenger.tuntap
{
    public sealed class TuntapDecenter : IDecenter
    {
        public string Name => "tuntap";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;
        public LinkerTunDeviceRouteItem[] Routes => routeItems;

        private VersionManager listVersion = new VersionManager();

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
        private LinkerTunDeviceRouteItem[] routeItems = new LinkerTunDeviceRouteItem[0];

        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly TuntapProxy tuntapProxy;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ExRouteTransfer exRouteTransfer;
        private readonly SignInClientState signInClientState;

        public TuntapDecenter(ISignInClientStore signInClientStore, SignInClientState signInClientState, ISerializer serializer, TuntapProxy tuntapProxy, TuntapConfigTransfer tuntapConfigTransfer, TuntapTransfer tuntapTransfer, ExRouteTransfer exRouteTransfer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.tuntapProxy = tuntapProxy;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapTransfer = tuntapTransfer;
            this.exRouteTransfer = exRouteTransfer;
            this.signInClientState = signInClientState;

        }

        public void Refresh()
        {
            PushVersion.Increment();
        }

        public Memory<byte> GetData()
        {
            return serializer.Serialize(new TuntapInfo
            {
                IP = tuntapConfigTransfer.Info.IP,
                Lans = tuntapConfigTransfer.Info.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false)
                .Select(c => { c.Exists = false; return c; }).ToList(),
                Wan = signInClientState.WanAddress.Address,
                PrefixLength = tuntapConfigTransfer.Info.PrefixLength,
                Name = tuntapConfigTransfer.Info.Name,
                MachineId = signInClientStore.Id,
                Status = tuntapTransfer.Status,
                SetupError = tuntapTransfer.SetupError,
                NatError = tuntapTransfer.NatError,
                SystemInfo = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}",

                Forwards = tuntapConfigTransfer.Info.Forwards,
                Switch = tuntapConfigTransfer.Info.Switch | (tuntapTransfer.AppNat ? TuntapSwitch.AppNat : 0)
            });
        }
        public void AddData(Memory<byte> data)
        {
            TuntapInfo info = serializer.Deserialize<TuntapInfo>(data.Span);
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            listVersion.Increment();
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<TuntapInfo> list = data.Select(c => serializer.Deserialize<TuntapInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                tuntapInfos.AddOrUpdate(item.MachineId, item, (a, b) => item);
            }
            DataVersion.Increment();
            listVersion.Increment();
        }
        public void ClearData()
        {
            tuntapInfos.Clear();
            tuntapProxy.ClearIPs();
        }
        public void ProcData()
        {
            AddRoute();
        }

        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(Infos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var _routeItems = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.PrefixLength }).ToArray();

            var removeItems = routeItems.Except(_routeItems, new LinkerTunDeviceRouteItemComparer()).ToArray();
            if (removeItems.Length > 0)
                tuntapTransfer.RemoveRoute(removeItems);

            tuntapTransfer.AddRoute(_routeItems);

            tuntapProxy.SetIPs(ips);
            foreach (var item in Infos.Values)
            {
                tuntapProxy.SetIP(item.MachineId, NetworkHelper.ToValue(item.IP));
            }
            foreach (var item in Infos.Values.Where(c => c.IP.Equals(IPAddress.Any)))
            {
                tuntapProxy.RemoveIP(item.MachineId);
            }

            routeItems = _routeItems;
        }
        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            //排除的IP，
            uint[] excludeIps = exRouteTransfer.Get().Where(c => c.Equals(IPAddress.Any) == false).Select(NetworkHelper.ToValue).Distinct().ToArray();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"tuntap route ex ips : {string.Join(",", excludeIps.Select(c => NetworkHelper.ToIP(c)).ToList())}");

            HashSet<uint> hashSet = new HashSet<uint>();
            IPAddress wan = signInClientState.WanAddress.Address;

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)
                .OrderBy(c => c.IP, new IPAddressComparer()).OrderByDescending(c => c.Status)
                .Select(c =>
                {
                    var lans = c.Lans
                    //未禁用 并且 设置了ip
                    .Where(d => d.Disabled == false && d.IP.Equals(IPAddress.Any) == false)
                    //不是同一个外网 或者 设置了映射
                    .Where(d => wan.Equals(c.Wan) == false || (d.MapIP != null && d.MapIP.Equals(IPAddress.Any) == false))
                    //未冲突
                    .Where(d =>
                    {
                        uint network = NetworkHelper.ToNetworkValue(d.IP, d.PrefixLength);
                        d.Exists = excludeIps.Any(e => NetworkHelper.ToNetworkValue(e, d.PrefixLength) == network) || hashSet.Contains(network);
                        hashSet.Add(network);
                        return d.Exists == false;
                    }).ToList();

                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(lans, c.MachineId),
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
            uint ipInt = NetworkHelper.ToValue(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.ToPrefixValue(prefixLength);
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
                return (int)(NetworkHelper.ToValue(x) - NetworkHelper.ToValue(y));
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
