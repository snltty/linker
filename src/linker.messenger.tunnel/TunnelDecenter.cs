using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using linker.plugins.tunnel;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
namespace linker.messenger.tunnel
{
    public sealed class TunnelDecenter : IDecenter
    {
        public string Name => "tunnel";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, TunnelRouteLevelInfo> Config { get; } = new ConcurrentDictionary<string, TunnelRouteLevelInfo>();

        private readonly ITunnelClientStore tunnelClientMessengerAdapterStore;
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;

        public TunnelDecenter(ITunnelClientStore tunnelClientMessengerAdapterStore, TunnelNetworkTransfer tunnelNetworkTransfer, ISerializer serializer, SignInClientState signInClientState)
        {
            this.tunnelClientMessengerAdapterStore = tunnelClientMessengerAdapterStore;
            tunnelClientMessengerAdapterStore.OnChanged += Refresh;
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
            this.serializer = serializer;
            this.signInClientState = signInClientState;
        }
        public void Refresh()
        {
            SyncVersion.Add();
        }
        public Memory<byte> GetData()
        {
            TunnelRouteLevelInfo tunnelTransportRouteLevelInfo = GetLocalRouteLevel();
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Add();
            return serializer.Serialize(tunnelTransportRouteLevelInfo);
        }
        public void SetData(Memory<byte> data)
        {
            TunnelRouteLevelInfo tunnelTransportRouteLevelInfo = serializer.Deserialize<TunnelRouteLevelInfo>(data.Span);
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<TunnelRouteLevelInfo> list = data.Select(c => serializer.Deserialize<TunnelRouteLevelInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                Config.AddOrUpdate(item.MachineId, item, (a, b) => item);
            }
            TunnelRouteLevelInfo config = GetLocalRouteLevel();
            Config.AddOrUpdate(config.MachineId, config, (a, b) => config);
            DataVersion.Add();
        }

        private TunnelRouteLevelInfo GetLocalRouteLevel()
        {
            return new TunnelRouteLevelInfo
            {
                MachineId = signInClientState.Connection?.Id ?? string.Empty,
                RouteLevel = tunnelNetworkTransfer.Info.RouteLevel,
                NeedReboot = false,
                PortMapLan = tunnelClientMessengerAdapterStore.PortMapPrivate,
                PortMapWan = tunnelClientMessengerAdapterStore.PortMapPublic,
                RouteLevelPlus = tunnelClientMessengerAdapterStore.RouteLevelPlus,
                HostName = Dns.GetHostName(),
                Lans = GetInterfaces(),
                Routes = tunnelNetworkTransfer.Info.RouteIPs,
            };
        }

        private static byte[] ipv6LocalBytes = new byte[] { 254, 128, 0, 0, 0, 0, 0, 0 };
        private TunnelInterfaceInfo[] GetInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Select(c => new TunnelInterfaceInfo
            {
                Name = c.Name,
                Desc = c.Description,
                Mac = Regex.Replace(c.GetPhysicalAddress().ToString(), @"(.{2})", $"$1-").Trim('-'),
                Ips = c.GetIPProperties().UnicastAddresses.Select(c => c.Address).Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || (c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(ipv6LocalBytes) == false)).ToArray()
            }).Where(c => c.Ips.Any(d => d.Equals(IPAddress.Loopback)) == false).ToArray();
        }
    }
}
