using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.messenger.exroute;
using linker.tun;
using System.Net;
using linker.libs.timer;

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
        private readonly ITuntapSystemInformation systemInformation;
        private readonly SignInClientTransfer signInClientTransfer;

        public TuntapDecenter(ISignInClientStore signInClientStore, SignInClientState signInClientState, ISerializer serializer, TuntapProxy tuntapProxy,
            TuntapConfigTransfer tuntapConfigTransfer, TuntapTransfer tuntapTransfer, ExRouteTransfer exRouteTransfer, ITuntapSystemInformation systemInformation,
            SignInClientTransfer signInClientTransfer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.tuntapProxy = tuntapProxy;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapTransfer = tuntapTransfer;
            this.exRouteTransfer = exRouteTransfer;
            this.signInClientState = signInClientState;
            this.systemInformation = systemInformation;
            this.signInClientTransfer = signInClientTransfer;

            CheckAvailableTask();
        }

        public void Refresh()
        {
            PushVersion.Increment();
        }

        public Memory<byte> GetData()
        {
            if (tuntapTransfer.AppNat)
            {
                tuntapConfigTransfer.Info.Switch |= TuntapSwitch.AppNat;
            }
            else
            {
                tuntapConfigTransfer.Info.Switch &= ~TuntapSwitch.AppNat;
            }
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
                SystemInfo = systemInformation.Get(),

                Forwards = tuntapConfigTransfer.Info.Forwards,
                Switch = tuntapConfigTransfer.Info.Switch
            });
        }
        public void AddData(Memory<byte> data)
        {
            TuntapInfo info = serializer.Deserialize<TuntapInfo>(data.Span);
            info.Available = true;
            if (tuntapInfos.TryGetValue(info.MachineId, out var old))
            {
                info.Delay = old.Delay;
            }
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            listVersion.Increment();
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<TuntapInfo> list = data.Select(c => serializer.Deserialize<TuntapInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                item.Available = true;
                if (tuntapInfos.TryGetValue(item.MachineId, out var old))
                {
                    item.Delay = old.Delay;
                }
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
            foreach (var item in Infos.Values.Where(c => c.Available && c.Exists == false))
            {
                tuntapProxy.SetIP(item.MachineId, NetworkHelper.ToValue(item.IP));
            }
            foreach (var item in Infos.Values.Where(c => c.Available == false || c.Exists || c.IP.Equals(IPAddress.Any)))
            {
                tuntapProxy.RemoveIP(item.MachineId);
            }

            routeItems = _routeItems;
        }
        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            //排除的IP，
            IEnumerable<uint> excludeIps = exRouteTransfer.Get().Where(c => c.Equals(IPAddress.Any) == false).Select(NetworkHelper.ToValue).Distinct();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"tuntap route ex ips : {string.Join(",", excludeIps.Select(c => NetworkHelper.ToIP(c)).ToList())}");

            HashSet<uint> hashSet = new HashSet<uint>();
            IPAddress wan = signInClientState.WanAddress.Address;

            foreach (var item in infos.Where(c => c.Available == true).OrderBy(c => c.IP, new IPAddressComparer()).OrderByDescending(c => c.Status))
            {
                item.Exists = item.IP.Equals(IPAddress.Any) == false && hashSet.Contains(NetworkHelper.ToValue(item.IP));
                hashSet.Add(NetworkHelper.ToValue(item.IP));
            }

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)
                .Where(c => c.Available && c.Exists == false)
                .OrderBy(c => c.IP, new IPAddressComparer()).OrderByDescending(c => c.Status)
                .Select(c =>
                {
                    var lans = c.Lans
                    //未禁用 并且 设置了ip
                    .Where(d => d.Disabled == false && d.IP.Equals(IPAddress.Any) == false)
                    //未冲突
                    .Where(d =>
                    {
                        uint network = NetworkHelper.ToNetworkValue(d.IP, d.PrefixLength);
                        //同个外网，且没有设置映射
                        d.Exists = (wan.Equals(c.Wan) && IPAddress.Any.Equals(d.MapIP))
                        //在排除列表中
                        || excludeIps.Any(e => NetworkHelper.ToNetworkValue(e, d.PrefixLength) == network)
                        //已经存在过
                        || hashSet.Contains(network);

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


        private void CheckAvailableTask()
        {
            ulong version = listVersion.Value;
            TimerHelper.SetIntervalLong(async () =>
            {
                if (listVersion.Eq(version, out ulong _version) == false)
                {
                    IEnumerable<string> availables = tuntapInfos.Values.Where(c => c.Available).Select(c => c.MachineId);
                    if (availables.Any())
                    {
                        List<string> offlines = await signInClientTransfer.GetOfflines(availables.ToList()).ConfigureAwait(false); ;
                        if (offlines.Any())
                        {
                            foreach (var item in tuntapInfos.Values.Where(c => offlines.Contains(c.MachineId)))
                            {
                                item.Available = false;
                            }
                            ProcData();
                        }
                    }
                }
                version = _version;
            }, 3000);
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
