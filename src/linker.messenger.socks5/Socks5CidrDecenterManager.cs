using linker.libs;
using linker.libs.extends;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.nat;
using System.Net;
using static linker.nat.LinkerDstMapping;

namespace linker.messenger.socks5
{
    public sealed class Socks5CidrDecenterManager
    {
        private readonly IPAddessCidrManager<string> cidrManager = new IPAddessCidrManager<string>();
        private readonly LinkerDstMapping mapping = new LinkerDstMapping();

        private readonly Socks5Decenter socks5Decenter;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly ExRouteTransfer exRouteTransfer;
        private readonly ISocks5Store socks5Store;
        public Socks5CidrDecenterManager(Socks5Decenter socks5Decenter, SignInClientState signInClientState, ISignInClientStore signInClientStore, ExRouteTransfer exRouteTransfer, ISocks5Store socks5Store)
        {
            this.socks5Decenter = socks5Decenter;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.exRouteTransfer = exRouteTransfer;
            this.socks5Store = socks5Store;

            socks5Decenter.OnChanged += AddRoute;
            socks5Decenter.OnClear += cidrManager.Clear;
            socks5Decenter.OnRefresh += SetMaps;
        }

        public IPAddress GetMapRealDst(IPAddress fakeIP)
        {
            return mapping.GetRealDst(fakeIP);
        }
        public bool FindValue(uint ip, out string value)
        {
            return cidrManager.FindValue(ip, out value);
        }

        private void SetMaps()
        {
            DstMapInfo[] maps = socks5Store.Lans
              .Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && c.MapIP != null && c.MapIP.Equals(IPAddress.Any) == false)
              .Select(c => new DstMapInfo { FakeIP = c.IP, RealIP = c.MapIP, PrefixLength = c.MapPrefixLength })
              .ToArray();
            mapping.SetDsts(maps);
        }
        private void AddRoute()
        {
            List<Socks5LanIPAddressList> ipsList = ParseIPs(socks5Decenter.Infos.Values.ToList());
            var routes = ipsList.SelectMany(c => c.IPS).Select(c => new CidrAddInfo<string> { IPAddress = c.IPAddress, PrefixLength = c.PrefixLength, Value = c.MachineId }).ToArray();
            cidrManager.Add(routes);
            GC.Collect();
        }

        private List<Socks5LanIPAddressList> ParseIPs(List<Socks5Info> infos)
        {
            //排除的IP，
            uint[] excludeIps = exRouteTransfer.Get().Concat(socks5Store.Lans.Select(c => c.IP))
                .Select(NetworkHelper.ToValue)
                .ToArray();

            HashSet<uint> hashSet = new HashSet<uint>();
            IPAddress wan = signInClientState.WanAddress.Address;

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)
                .OrderByDescending(c => c.Status)
                .OrderByDescending(c => c.MachineId)

                .Select(c =>
                {
                    var lans = c.Lans.Where(d => d.Disabled == false && d.IP.Equals(IPAddress.Any) == false).Where(d =>
                    {
                        uint ipInt = NetworkHelper.ToValue(d.IP);
                        uint maskValue = NetworkHelper.ToPrefixValue(d.PrefixLength);
                        uint network = ipInt & maskValue;

                        if (d.IP.Equals(d.MapIP))
                        {
                            d.Exists = hashSet.Contains(network);
                        }
                        else
                        {
                            d.Exists = (wan.Equals(c.Wan) && IPAddress.Any.Equals(d.MapIP))
                            || excludeIps.Any(e => (e & maskValue) == network)
                            || hashSet.Contains(network);
                        }
                        hashSet.Add(network);
                        return d.Exists == false;
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
            uint ipInt = NetworkHelper.ToValue(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.ToPrefixValue(prefixLength);
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
