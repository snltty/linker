using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using linker.plugins.tunnel;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
namespace linker.messenger.tunnel
{
    public sealed class TunnelDecenter : IDecenter
    {
        public string Name => "tunnel";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, TunnelRouteLevelInfo> Config { get; } = new ConcurrentDictionary<string, TunnelRouteLevelInfo>();

        private readonly ITunnelClientStore tunnelClientStore;
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;

        public TunnelDecenter(ITunnelClientStore tunnelClientStore, TunnelNetworkTransfer tunnelNetworkTransfer, ISerializer serializer, SignInClientState signInClientState)
        {
            this.tunnelClientStore = tunnelClientStore;
            tunnelClientStore.OnChanged += Refresh;
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
            this.serializer = serializer;
            this.signInClientState = signInClientState;

        }
        public void Refresh()
        {
            PushVersion.Increment();
        }
        public Memory<byte> GetData()
        {
            TunnelRouteLevelInfo tunnelTransportRouteLevelInfo = GetLocalRouteLevel();
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Increment();
            return serializer.Serialize(tunnelTransportRouteLevelInfo);
        }
        public void SetData(Memory<byte> data)
        {
            TunnelRouteLevelInfo tunnelTransportRouteLevelInfo = serializer.Deserialize<TunnelRouteLevelInfo>(data.Span);
            Config.AddOrUpdate(tunnelTransportRouteLevelInfo.MachineId, tunnelTransportRouteLevelInfo, (a, b) => tunnelTransportRouteLevelInfo);
            DataVersion.Increment();
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
            DataVersion.Increment();
        }

        private TunnelRouteLevelInfo GetLocalRouteLevel()
        {
            return new TunnelRouteLevelInfo
            {
                MachineId = signInClientState.Connection?.Id ?? string.Empty,
                RouteLevel = tunnelNetworkTransfer.Info.RouteLevel,
                NeedReboot = false,
                PortMapLan = tunnelClientStore.PortMapPrivate,
                PortMapWan = tunnelClientStore.PortMapPublic,
                RouteLevelPlus = tunnelClientStore.RouteLevelPlus,
                Net = tunnelNetworkTransfer.Info.Net
            };
        }
       

       
    }
}
