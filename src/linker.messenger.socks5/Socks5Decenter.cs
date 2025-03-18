using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using System.Net;
using linker.messenger.signin;
using linker.messenger.exroute;
using linker.libs.timer;
using System.Threading;

namespace linker.messenger.socks5
{
    public sealed class Socks5Decenter : IDecenter
    {
        public string Name => "socks5";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        private readonly ConcurrentDictionary<string, Socks5Info> socks5Infos = new ConcurrentDictionary<string, Socks5Info>();
        public ConcurrentDictionary<string, Socks5Info> Infos => socks5Infos;

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

            AddRouteTask();
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
                Lans = socks5Store.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false; return c; }).ToList(),
                MachineId = signInClientStore.Id,
                Status = tunnelProxy.Running ? Socks5Status.Running : Socks5Status.Normal,
                Port = socks5Store.Port,
                SetupError = tunnelProxy.Error
            };
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            DataVersion.Add();
            return serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            Socks5Info info = serializer.Deserialize<Socks5Info>(data.Span);
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<Socks5Info> list = data.Select(c => serializer.Deserialize<Socks5Info>(c.Span)).ToList();
            foreach (var item in list)
            {
                socks5Infos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                item.LastTicks.Update();
            }
            DataVersion.Add();
        }

        private void AddRouteTask()
        {
            ulong version = 0;
            TimerHelper.SetIntervalLong(() =>
            {
                if(DataVersion.Eq(version,out ulong _version) == false)
                {
                    AddRoute();
                }
                version = _version;
            }, 3000);
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
            uint[] excludeIps = exRouteTransfer.Get().Concat(socks5Store.Lans.Select(c => c.IP))
                .Select(NetworkHelper.ToValue)
                .ToArray();

            HashSet<uint> hashSet = new HashSet<uint>();

            return infos
                .Where(c => c.MachineId != signInClientStore.Id)
                .OrderByDescending(c => c.Status)
                .OrderByDescending(c => c.MachineId)

                .Select(c =>
                {
                    var lans = c.Lans.Where(c => c.Disabled == false && c.IP.Equals(IPAddress.Any) == false).Where(c =>
                    {
                        uint ipInt = NetworkHelper.ToValue(c.IP);
                        uint maskValue = NetworkHelper.ToPrefixValue(c.PrefixLength);
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
