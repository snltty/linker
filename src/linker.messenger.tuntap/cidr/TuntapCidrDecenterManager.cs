using linker.libs;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.tun.device;
using System.Net;

namespace linker.messenger.tuntap.cidr
{
    public sealed class TuntapCidrDecenterManager
    {
        private LinkerTunDeviceRouteItem[] routeItems = new LinkerTunDeviceRouteItem[0];
        public LinkerTunDeviceRouteItem[] Routes => routeItems;
        public Dictionary<string, string> CidrRoutes => cidrManager.Routes;

        private readonly IPAddessCidrManager<string> cidrManager = new IPAddessCidrManager<string>();
        private readonly TuntapCidrConnectionManager tuntapCidrConnectionManager;

        private readonly ISignInClientStore signInClientStore;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ExRouteTransfer exRouteTransfer;
        private readonly SignInClientState signInClientState;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly TuntapDecenter tuntapDecenter;

        public TuntapCidrDecenterManager(TuntapCidrConnectionManager tuntapCidrConnectionManager, ISignInClientStore signInClientStore, SignInClientState signInClientState, TuntapConfigTransfer tuntapConfigTransfer, TuntapTransfer tuntapTransfer, ExRouteTransfer exRouteTransfer, SignInClientTransfer signInClientTransfer, TuntapDecenter tuntapDecenter)
        {
            this.tuntapCidrConnectionManager = tuntapCidrConnectionManager;
            this.signInClientStore = signInClientStore;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapTransfer = tuntapTransfer;
            this.exRouteTransfer = exRouteTransfer;
            this.signInClientState = signInClientState;
            this.signInClientTransfer = signInClientTransfer;
            this.tuntapDecenter = tuntapDecenter;

            tuntapDecenter.OnClear += Clear;
            tuntapDecenter.OnChanged += AddRoute;
        }

        public void SetIPs(TuntapVeaLanIPAddress[] ips)
        {
            foreach (var ip in ips)
            {
                tuntapCidrConnectionManager.RemoveNotMachine(ip.NetWork, ip.MaskValue, ip.MachineId);
            }
            cidrManager.Add(ips.Select(c => new CidrAddInfo<string> { IPAddress = c.IPAddress, PrefixLength = c.PrefixLength, Value = c.MachineId }).ToArray());

        }
        public void SetIP(string machineId, uint ip)
        {
            cidrManager.Add(new CidrAddInfo<string> { IPAddress = ip, PrefixLength = 32, Value = machineId });
            tuntapCidrConnectionManager.RemoveNotMachine(ip, machineId);
        }
        public void RemoveIP(string machineId)
        {
            cidrManager.Delete(machineId, (a, b) => a == b);
            tuntapCidrConnectionManager.Remove(machineId);
        }
        public bool FindValue(uint ip, out string value)
        {
            return cidrManager.FindValue(ip, out value);
        }
        private void Clear()
        {
            cidrManager.Clear();
            tuntapCidrConnectionManager.Clear();
        }

        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapDecenter.Infos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var _routeItems = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.PrefixLength }).ToArray();

            var removeItems = routeItems.Except(_routeItems, new LinkerTunDeviceRouteItemComparer()).ToArray();
            if (removeItems.Length > 0)
                tuntapTransfer.RemoveRoute(removeItems);

            tuntapTransfer.AddRoute(_routeItems);

            SetIPs(ips);
            foreach (var item in tuntapDecenter.Infos.Values.Where(c => c.Available && c.Exists == false))
            {
                SetIP(item.MachineId, NetworkHelper.ToValue(item.IP));
            }
            foreach (var item in tuntapDecenter.Infos.Values.Where(c => c.Available == false || c.Exists || c.IP.Equals(IPAddress.Any)))
            {
                RemoveIP(item.MachineId);
            }

            routeItems = _routeItems;

            GC.Collect();
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

                        if (d.IP.Equals(d.MapIP))
                        {
                            d.Exists = hashSet.Contains(network);
                        }
                        else
                        {
                            //同个外网，且没有设置映射
                            d.Exists = (wan.Equals(c.Wan) && IPAddress.Any.Equals(d.MapIP))
                            //在排除列表中
                            || excludeIps.Any(e => NetworkHelper.ToNetworkValue(e, d.PrefixLength) == network)
                            //已经存在过
                            || hashSet.Contains(network);
                        }
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
