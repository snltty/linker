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
        /// <summary>
        /// 中继配置
        /// </summary>
        public RelayConfigServerInfo Relay { get; set; } = new RelayConfigServerInfo();
    }
    public sealed class RelayConfigServerInfo
    {
        public DistributedInfo Distributed { get; set; } = new DistributedInfo { };
    }

    public sealed class DistributedInfo
    {
        public RelayServerNodeInfo Node { get; set; } = new RelayServerNodeInfo { };
        public RelayServerMasterInfo Master { get; set; } = new RelayServerMasterInfo { };
    }

}
