using System.Net;
using linker.libs.extends;
using System.Text.Json.Serialization;
using linker.tunnel.connection;

namespace linker.messenger.relay.server
{
    public interface IRelayServerConfigStore
    {
        public int ServicePort { get; }

        /// <summary>
        /// 节点信息
        /// </summary>
        public RelayServerConfigInfo Config { get; }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="config"></param>
        public void SetInfo(RelayServerConfigInfo config);

        /// <summary>
        /// 设置月份
        /// </summary>
        /// <param name="month"></param>
        public void SetDataMonth(int month);
        /// <summary>
        /// 设置剩余流量
        /// </summary>
        /// <param name="value"></param>
        public void SetDataRemain(long value);

        public void SetShareKey(string shareKey);

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

        public TunnelProtocolType Protocol { get; set; } = TunnelProtocolType.Tcp;
        public int Connections { get; set; } = 1000;
        public int Bandwidth { get; set; }
        public int DataEachMonth { get; set; }
        public long DataRemain { get; set; }

        public string Url { get; set; } = "https://linker.snltty.com";
        public string Logo { get; set; } = "https://linker.snltty.com/img/logo.png";
    }

    public sealed class RelayServerConfigInfo : RelayServerNodeInfo
    {
        public string ShareKey { get; set; } = string.Empty;
        public int DataMonth { get; set; }
        public string Domain { get; set; } = string.Empty;
    }

    public class RelayServerNodeReportInfo : RelayServerNodeInfo
    {
        public string Version { get; set; } = string.Empty;
        public int ConnectionsRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public IPEndPoint[] Servers { get; set; } = Array.Empty<IPEndPoint>();
    }

    public sealed class RelayServerNodeStoreInfo : RelayServerNodeReportInfo
    {
        public int Id { get; set; }

        public string Host { get; set; } = string.Empty;

        public int BandwidthEachConnection { get; set; } = 50;
        public bool Public { get; set; }

        public long LastTicks { get; set; }

        public int Delay { get; set; }
    }


    public class RelayServerNodeShareInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
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
