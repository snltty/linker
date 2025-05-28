using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using System.Net;
using linker.messenger.signin;
using linker.messenger.exroute;
using static linker.snat.LinkerDstMapping;

namespace linker.messenger.socks5
{
    public sealed class Socks5Decenter : IDecenter
    {
        public string Name => "socks5";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();

        private readonly ConcurrentDictionary<string, Socks5Info> socks5Infos = new ConcurrentDictionary<string, Socks5Info>();
        public ConcurrentDictionary<string, Socks5Info> Infos => socks5Infos;

        private VersionManager listVersion = new VersionManager();

        private readonly SignInClientState signInClientState;
        private readonly TunnelProxy tunnelProxy;
        private readonly ISignInClientStore signInClientStore;
        private readonly Socks5Transfer socks5Transfer;
        private readonly ExRouteTransfer exRouteTransfer;
        private readonly ISerializer serializer;
        private readonly ISocks5Store socks5Store;

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);

        public Socks5Decenter(SignInClientState signInClientState, TunnelProxy tunnelProxy, ISignInClientStore signInClientStore, Socks5Transfer socks5Transfer, ExRouteTransfer exRouteTransfer, ISerializer serializer, ISocks5Store socks5Store)
        {
            this.signInClientState = signInClientState;
            this.tunnelProxy = tunnelProxy;
            this.signInClientStore = signInClientStore;
            this.socks5Transfer = socks5Transfer;
            this.exRouteTransfer = exRouteTransfer;
            this.serializer = serializer;
            this.socks5Store = socks5Store;

            signInClientState.OnSignInSuccess += (times) => Refresh();
            tunnelProxy.RefreshConfig += Refresh;
            socks5Transfer.OnChanged += Refresh;
        }

        /// <summary>
        /// 刷新信息，把自己的配置发给别人，顺便把别人的信息带回来
        /// </summary>
        public void Refresh()
        {
            PushVersion.Increment();

            DstMapInfo[] maps = socks5Store.Lans
                .Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && c.MapIP != null && c.MapIP.Equals(IPAddress.Any) == false)
                .Select(c => new DstMapInfo { FakeIP = c.IP, RealIP = c.MapIP, PrefixLength = c.MapPrefixLength })
                .ToArray();
            tunnelProxy.SetMap(maps);
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(new Socks5Info
            {
                Lans = socks5Store.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false; return c; }).ToList(),
                MachineId = signInClientStore.Id,
                Status = tunnelProxy.Running ? Socks5Status.Running : Socks5Status.Normal,
                Port = socks5Store.Port,
                SetupError = tunnelProxy.Error,
                Wan = signInClientState.WanAddress.Address
            });
        }
        public void AddData(Memory<byte> data)
        {
            Socks5Info info = serializer.Deserialize<Socks5Info>(data.Span);
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            listVersion.Increment();
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<Socks5Info> list = data.Select(c => serializer.Deserialize<Socks5Info>(c.Span)).ToList();
            foreach (var item in list)
            {
                socks5Infos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                item.LastTicks.Update();
            }
            listVersion.Increment();
        }
        public void ClearData()
        {
            socks5Infos.Clear();
            tunnelProxy.ClearIPs();
        }
        public void ProcData()
        {
            AddRoute();
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<Socks5LanIPAddressList> ipsList = ParseIPs(socks5Infos.Values.ToList());
            Socks5LanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();

            tunnelProxy.SetIPs(ips);

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

                        d.Exists = (wan.Equals(c.Wan) && IPAddress.Any.Equals(d.MapIP))
                        || excludeIps.Any(e => (e & maskValue) == network)
                        || hashSet.Contains(network);
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
