using linker.libs;
using linker.messenger.relay.client.transport;
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
        public string DefaultNodeId { get; set; }
        public TunnelProtocolType DefaultProtocol { get; set; } = TunnelProtocolType.None;
    }

    public sealed partial class ConfigClientInfo
    {
        public RelayClientInfo Relay { get; set; } = new RelayClientInfo();
    }
    public sealed class RelayClientInfo
    {
        /// <summary>
        /// 中继服务器列表
        /// </summary>
        public RelayServerInfo[] Servers { get; set; } = new RelayServerInfo[] { new RelayServerInfo { } };

        [SaveJsonIgnore]
        public RelayServerInfo Server => Servers[0];

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

    public sealed class CdkeyConfigInfo
    {
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; }  = Guid.NewGuid().ToString().ToUpper();
#endif
    }
}
