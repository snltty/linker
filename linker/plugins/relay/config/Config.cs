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
        public RelayServerInfo[] Servers { get; set; } = new RelayServerInfo[] { new RelayServerInfo { Delay = -1 } };
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
        public string Type { get; set; } = "master";
        public RelayCachingInfo Caching { get; set; } = new RelayCachingInfo { };
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
        public string Name { get; set; } = "default node";
        public string Host { get; set; } = string.Empty;

        public string MasterHost { get; set; } = string.Empty;
        public string MasterSecretKey { get; set; } = Helper.GlobalString;

        public int MaxConnection { get; set; } = 100;
        public int MaxBandwidth { get; set; } = 500;

        public bool Public { get; set; }
       
    }
    public sealed class RelayCachingInfo
    {
        public string Type { get; set; } = "memory";
        public string ConnectString { get; set; } = string.Empty;
    }


    [MemoryPackable]
    public sealed partial class RelayNodeReportInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double ConnectionRatio { get; set; }
        public double BandwidthRatio { get; set; }
        public bool Public { get; set; }


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
        /// <summary>
        /// 延迟
        /// </summary>
        public int Delay { get; set; }

        public RelayType RelayType { get; set; } = RelayType.Linker;
    }

}
