using linker.libs;
using linker.plugins.relay.client.transport;
using MemoryPack;
using System.Net;

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
        public RelayNodeInfo Node { get; set; } = new RelayNodeInfo { };
        public RelayMasterInfo Master { get; set; } = new RelayMasterInfo { };
    }
    public sealed class RelayMasterInfo
    {
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }
    public sealed class RelayNodeInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string Name { get; set; } = "default";
        public string Host { get; set; } = string.Empty;

        public string MasterHost { get; set; } = string.Empty;
        public string MasterSecretKey { get; set; } = string.Empty;

        public int MaxConnection { get; set; } = 100;
        public int MaxBandwidth { get; set; } = 1;

        public bool Public { get; set; }

    }

    [MemoryPackable]
    public sealed partial class RelayNodeReportInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double ConnectionRatio { get; set; }
        public double BandwidthRatio { get; set; }
        public bool Public { get; set; }

        public int Delay { get; set; } = -1;

        [MemoryPackAllowSerialize]
        public IPEndPoint EndPoint { get; set; }
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

        public RelayType RelayType { get; set; } = RelayType.Linker;
    }

}
