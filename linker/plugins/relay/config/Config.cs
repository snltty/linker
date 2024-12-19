using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
using MemoryPack;
using System.Net;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public RelayInfo Relay { get; set; } = new RelayInfo();
    }

    public sealed class RelayInfo
    {
        public string DefaultNodeId { get; set; }
    }

}


namespace linker.config
{

    public sealed partial class ConfigClientInfo
    {
        public RelayInfo Relay { get; set; } = new RelayInfo();
    }
    public sealed class RelayInfo
    {
        /// <summary>
        /// 中继服务器列表
        /// </summary>
        public RelayServerInfo[] Servers { get; set; } = new RelayServerInfo[] { new RelayServerInfo { } };
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
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
        public DistributedInfo Distributed { get; set; } = new DistributedInfo { };
    }

    public sealed class DistributedInfo
    {
        public RelayServerNodeInfo Node { get; set; } = new RelayServerNodeInfo { };
        public RelayServerMasterInfo Master { get; set; } = new RelayServerMasterInfo { };
    }
    /// <summary>
    /// 中继服务器
    /// </summary>
    [MemoryPackable]
    public sealed partial class RelayServerInfo
    {
        public RelayServerInfo() { }
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = Helper.GlobalString;
        /// <summary>
        /// 禁用
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// 开启ssl
        /// </summary>
        public bool SSL { get; set; } = true;

        public RelayClientType RelayType { get; set; } = RelayClientType.Linker;
    }

}
