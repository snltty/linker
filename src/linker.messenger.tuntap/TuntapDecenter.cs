using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.messenger.exroute;
using linker.tun;
using System.Net;
using linker.libs.timer;
using System.Threading;

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

            signInClientState.OnSignInSuccess += NetworkEnable;
            AddRouteTask();

        }
        string groupid = string.Empty;
        private void NetworkEnable(int times)
        {
            if (groupid != signInClientStore.Group.Id)
            {
                tuntapInfos.Clear();
                tuntapProxy.ClearIPs();
            }
            groupid = signInClientStore.Group.Id;
        }

        public void Refresh()
        {
            PushVersion.Increment();
        }

        private TuntapInfo GetCurrentInfo()
        {
            return new TuntapInfo
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
                Switch = tuntapConfigTransfer.Info.Switch
            };
        }
        public Memory<byte> GetData()
        {
            TuntapInfo info = GetCurrentInfo();
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Debug($"tuntap decenter getdata");
            }
            DataVersion.Increment();
            return serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            TuntapInfo info = serializer.Deserialize<TuntapInfo>(data.Span);
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            DataVersion.Increment();
            listVersion.Increment();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<TuntapInfo> list = data.Select(c => serializer.Deserialize<TuntapInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                tuntapInfos.AddOrUpdate(item.MachineId, item, (a, b) => item);
            }
            DataVersion.Increment();
            listVersion.Increment();
        }

        private void AddRouteTask()
        {
            ulong version = 0;
            TimerHelper.SetIntervalLong(() =>
            {
                if (listVersion.Eq(version, out ulong _version) == false)
                {
                    AddRoute();
                }
                version = _version;
            }, 3000);
        }
        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(Infos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var _routeItems = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.PrefixLength }).ToArray();

            var removeItems = routeItems.Except(_routeItems, new LinkerTunDeviceRouteItemComparer()).ToArray();
            if (removeItems.Length > 0)
                tuntapTransfer.DelRoute(removeItems);

            tuntapTransfer.AddRoute(_routeItems, tuntapConfigTransfer.Info.IP);

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
            uint[] excludeIps = exRouteTransfer.Get().Select(NetworkHelper.ToValue).Distinct().ToArray();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"tuntap route ex ips : {string.Join(",", excludeIps.Select(c => NetworkHelper.ToIP(c)).ToList())}");

            HashSet<uint> hashSet = new HashSet<uint>();

            IPAddress wan = signInClientState.WanAddress.Address;

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)

                .Where(c =>
                {
                    if (wan.Equals(c.Wan))
                    {
                        foreach (var item in c.Lans)
                        {
                            item.Exists = true;
                        }
                        return false;
                    }
                    return true;
                })

                .OrderBy(c => c.IP, new IPAddressComparer()).OrderByDescending(c => c.Status)
                .Select(c =>
                {
                    var lans = c.Lans.Where(c => c.Disabled == false && c.IP.Equals(IPAddress.Any) == false).Where(c =>
                    {
                        uint network = NetworkHelper.ToNetworkValue(c.IP, c.PrefixLength);
                        c.Exists = excludeIps.Any(d => NetworkHelper.ToNetworkValue(d, c.PrefixLength) == network) || hashSet.Contains(network);
                        hashSet.Add(network);
                        return c.Exists == false;
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
