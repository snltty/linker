using linker.libs.extends;
using linker.messenger.node;
using linker.tunnel.connection;
using System.Net;
using System.Text.Json.Serialization;

namespace linker.messenger.relay.server
{
    public interface IRelayNodeStore : INodeStore<RelayServerNodeStoreInfo, RelayServerNodeReportInfo>
    {
    }
    public interface IRelayNodeConfigStore : INodeConfigStore<RelayServerConfigInfo>
    {

    }

    public class RelayServerNodeInfo
    {
        private string nodeId = Guid.NewGuid().ToString().ToUpper();
        public string NodeId { get => nodeId; set { nodeId = value.SubStr(0, 36); } }

        private string name = "default";
        public string Name { get => name; set { name = value.SubStr(0, 32); } }

        public string Host { get; set; } = string.Empty;

        public TunnelProtocolType Protocol { get; set; } = TunnelProtocolType.Tcp;
        public int Connections { get; set; }
        public int Bandwidth { get; set; }
        public int DataEachMonth { get; set; }
        public long DataRemain { get; set; }

        public string Url { get; set; } = "https://linker.snltty.com";
        public string Logo { get; set; } = "https://linker.snltty.com/img/logo.png";
    }

    public sealed class RelayServerConfigInfo : RelayServerNodeInfo, INodeConfigBase
    {
        [SaveJsonIgnore]
        public DistributedInfoOld Distributed { get; set; } = new DistributedInfoOld { };

        public string ShareKey { get; set; } = string.Empty;
        public string ShareKeyManager { get; set; } = string.Empty;
        public string MasterKey { get; set; } = Guid.NewGuid().ToString().Md5();
        public int DataMonth { get; set; }


    }

    public class RelayServerNodeReportInfo : RelayServerNodeInfo, INodeReportBase
    {
        public string MasterKey { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int ConnectionsRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public int MasterCount { get; set; }
    }

    public sealed class RelayServerNodeStoreInfo : RelayServerNodeReportInfo, INodeStoreBase
    {
        public int Id { get; set; }

        public int BandwidthEach { get; set; } = 50;
        public bool Public { get; set; }

        public long LastTicks { get; set; }

        public int Delay { get; set; }

        public bool Manageable { get; set; }
        public string ShareKey { get; set; } = string.Empty;
    }

    public sealed class DistributedInfoOld
    {
        public RelayServerNodeInfoOld Node { get; set; } = new RelayServerNodeInfoOld { };
    }
    public sealed class RelayServerNodeInfoOld
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Host { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }
        public int MaxGbTotalMonth { get; set; }

        public string Url { get; set; } = "https://linker-doc.snltty.com";

    }
    public partial class RelayServerNodeReportInfoOld
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }

        public double ConnectionRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public bool Public { get; set; }

        public int Delay { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public long LastTicks { get; set; }

        /// <summary>
        /// 170+
        /// </summary>
        public string Url { get; set; } = "https://linker-doc.snltty.com";
        public TunnelProtocolType AllowProtocol { get; set; } = TunnelProtocolType.Tcp;

        /// <summary>
        /// 188+
        /// </summary>
        public string Version { get; set; } = string.Empty;
        public bool Sync2Server { get; set; }

        [JsonIgnore]
        public IConnection Connection { get; set; }
    }

}
