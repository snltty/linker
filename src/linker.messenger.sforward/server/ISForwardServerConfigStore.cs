using linker.libs.extends;
using linker.messenger.node;
using System.Net;
using System.Text.Json.Serialization;

namespace linker.messenger.sforward.server
{
    public interface ISForwardNodeStore : INodeStore<SForwardServerNodeStoreInfo, SForwardServerNodeReportInfo>
    {
    }
    public interface ISForwardNodeConfigStore : INodeConfigStore<SForwardServerConfigInfo>
    {

    }
    public class SForwardServerNodeInfo
    {
        private string nodeId = Guid.NewGuid().ToString().ToUpper();
        public string NodeId { get => nodeId; set { nodeId = value.SubStr(0, 36); } }

        private string name = "default";
        public string Name { get => name; set { name = value.SubStr(0, 32); } }

        public string Domain { get; set; } = string.Empty;

        public int WebPort { get; set; }
        public string TunnelPorts { get; set; } = "1024-65535";

        public int Connections { get; set; }
        public int Bandwidth { get; set; }
        public int DataEachMonth { get; set; }
        public long DataRemain { get; set; }

        public string Url { get; set; } = "https://linker.snltty.com";
        public string Logo { get; set; } = "https://linker.snltty.com/img/logo.png";
    }

    public sealed class SForwardServerConfigInfo : SForwardServerNodeInfo, INodeConfigBase
    {
        [SaveJsonIgnore]
        public DistributedInfoOld Distributed { get; set; } = new DistributedInfoOld { };

        public string ShareKey { get; set; } = string.Empty;
        public string ShareKeyManager { get; set; } = string.Empty;
        public string MasterKey { get; set; } = Guid.NewGuid().ToString().Md5();
        public int DataMonth { get; set; }
        public string Host { get; set; } = string.Empty;
    }

    public class SForwardServerNodeReportInfo : SForwardServerNodeInfo, INodeReportBase
    {
        public string MasterKey { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int ConnectionsRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public int MasterCount { get; set; }

        public string Host { get; set; } = string.Empty;
    }

    public sealed class SForwardServerNodeStoreInfo : SForwardServerNodeReportInfo, INodeStoreBase
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
        public SForwardServerNodeInfoOld Node { get; set; } = new SForwardServerNodeInfoOld { };
    }
    public sealed class SForwardServerNodeInfoOld
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;

        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }
        public int MaxGbTotalMonth { get; set; }
        public string Url { get; set; } = "https://linker-doc.snltty.com";
    }
    public partial class SForwardServerNodeReportInfoOld
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }

        public double BandwidthRatio { get; set; }

        public bool Public { get; set; }

        public int Delay { get; set; }

        public string Domain { get; set; }
        public IPAddress Address { get; set; }

        public long LastTicks { get; set; }
        public string Url { get; set; } = "https://linker-doc.snltty.com";
        [JsonIgnore]
        public IConnection Connection { get; set; }
        public string Version { get; set; } = string.Empty;
        public bool Sync2Server { get; set; }

        public int WebPort { get; set; }
        public int[] PortRange { get; set; } = [1025, 65535];
    }
}
