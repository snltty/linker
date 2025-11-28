using linker.messenger.relay.server;
using linker.tunnel.connection;


namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        public RelayInfo Relay { get; set; } = new RelayInfo();

    }
    public sealed class RelayInfo
    {
        public string DefaultNodeId { get; set; } = string.Empty;
        public TunnelProtocolType DefaultProtocol { get; set; } = TunnelProtocolType.None;
    }

    public partial class ConfigServerInfo
    {
        public RelayServerConfigInfo Relay { get; set; } = new RelayServerConfigInfo();
    }

}
