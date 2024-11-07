using linker.libs;
using linker.libs.extends;
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
        public const string MASTER_NODE_ID = "824777CF-2804-83FE-DE71-69B7B7D3BBA7";

        private string id = Guid.NewGuid().ToString().ToUpper();
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

        private string name = Dns.GetHostName().SubStr(0, 12);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 12);
            }
        }
        public string Host { get; set; } = string.Empty;

        public int MaxConnection { get; set; } = 100;
        public double MaxBandwidth { get; set; } = 1;
        public bool Public { get; set; }

        public string MasterHost { get; set; } = string.Empty;

#if DEBUG
        public string MasterSecretKey { get; set; } = Helper.GlobalString;
#else
        public string MasterSecretKey { get; set; } = string.Empty;
#endif
    }

    [MemoryPackable]
    public sealed partial class RelayNodeReportInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidth { get; set; }
        public double ConnectionRatio { get; set; }
        public double BandwidthRatio { get; set; }
        public bool Public { get; set; }

        public int Delay { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint EndPoint { get; set; }

        public long LastTicks { get; set; }
    }

    [MemoryPackable]
    public sealed partial class RelayNodeDelayInfo
    {
        public string Id { get; set; } = string.Empty;
        public int Delay { get; set; }

        [MemoryPackAllowSerialize]
        public IPAddress IP { get; set; }
    }
    [MemoryPackable]
    public sealed partial class RelayNodeDelayWrapInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public Dictionary<string, RelayNodeDelayInfo> Nodes { get; set; }
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
