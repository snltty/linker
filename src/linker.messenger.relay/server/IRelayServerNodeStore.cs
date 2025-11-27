using linker.libs;
using System.Net;
using linker.libs.extends;
using System.Text.Json.Serialization;
using linker.tunnel.connection;

namespace linker.messenger.relay.server
{
    public interface IRelayServerNodeStore
    {
        /// <summary>
        /// 服务端端口
        /// </summary>
        public int ServicePort { get; }
        /// <summary>
        /// 节点信息
        /// </summary>
        public RelayServerNodeInfo Node { get; }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="node"></param>
        public void SetInfo(RelayServerNodeInfo node);

        /// <summary>
        /// 设置月份
        /// </summary>
        /// <param name="month"></param>
        public void SetMaxGbTotalMonth(int month);
        /// <summary>
        /// 设置剩余流量
        /// </summary>
        /// <param name="value"></param>
        public void SetMaxGbTotalLastBytes(long value);
        /// <summary>
        /// 提交保存
        /// </summary>
        public void Confirm();
    }

    public class RelayServerNodeInfo
    {
        private string nodeId = Guid.NewGuid().ToString().ToUpper();
        public string NodeId { get => nodeId; set { nodeId = value.SubStr(0, 36); } }

        private string name = Dns.GetHostName().SubStr(0, 32);
        public string Name { get => name; set { name = value.SubStr(0, 32); } }
        public string Host { get; set; } = string.Empty;

        public TunnelProtocolType Protocol { get; set; } = TunnelProtocolType.Tcp;
        public int Connections { get; set; } = 1000;
        public int Bandwidth { get; set; } = 50;
        public int DataEachMonth { get; set; } = 100;
        public long DataRemain { get; set; }
        public int DataMonth { get; set; }
        public string Url { get; set; } = "https://linker.snltty.com";
        public string Logo { get; set; } = "https://linker.snltty.com/img/logo.png";
    }

    public class RelayServerNodeReportInfo : RelayServerNodeInfo
    {
        public string Version { get; set; } = string.Empty;
        public int ConnectionsRatio { get; set; }
        public int BandwidthRatio { get; set; }
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
