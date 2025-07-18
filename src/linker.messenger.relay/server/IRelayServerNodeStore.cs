using linker.libs;
using System.Net;
using linker.libs.extends;
using System.Text.Json.Serialization;
using linker.tunnel.connection;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继节点存储器
    /// </summary>
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
        public void UpdateInfo(RelayServerNodeUpdateInfo188 update);

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


    public sealed class RelayServerNodeInfo
    {
        private string id = Guid.NewGuid().ToString().ToUpper();
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

        private string name = Dns.GetHostName().SubStr(0, 32);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 32);
            }
        }
        public string Host { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }
        public int MaxGbTotalMonth { get; set; }

        public bool Public { get; set; }

        public string MasterHost { get; set; } = string.Empty;
#if DEBUG
        public string MasterSecretKey { get; set; } = Helper.GlobalString;
#else
        public string MasterSecretKey { get; set; } = string.Empty;
#endif
        public string Url { get; set; } = "https://linker-doc.snltty.com";

        public bool AllowTcp { get; set; } = true;
        public bool AllowUdp { get; set; }
        public bool Sync2Server { get; set; } 
    }

    public partial class RelayServerNodeUpdateWrapInfo
    {
        public RelayServerNodeUpdateInfo Info { get; set; }
    }
    public partial class RelayServerNodeUpdateInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }
        public bool Public { get; set; }

        public string Url { get; set; } = "https://linker-doc.snltty.com";

        public bool AllowTcp { get; set; } = true;
        public bool AllowUdp { get; set; } = false;
    }

    public sealed partial class RelayServerNodeUpdateWrapInfo188
    {
        public RelayServerNodeUpdateInfo188 Info { get; set; }
    }
    public sealed partial class RelayServerNodeUpdateInfo188 : RelayServerNodeUpdateInfo
    {
        public bool Sync2Server { get; set; } = false;
    }


    public partial class RelayServerNodeReportInfo
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
    }
    public partial class RelayServerNodeReportInfo170 : RelayServerNodeReportInfo
    {
        public string Url { get; set; } = "https://linker-doc.snltty.com";
        public TunnelProtocolType AllowProtocol { get; set; } = TunnelProtocolType.Tcp;

        [JsonIgnore]
        public IConnection Connection { get; set; }
    }
    public partial class RelayServerNodeReportInfo188 : RelayServerNodeReportInfo170
    {
        public string Version { get; set; } = string.Empty;
        public bool Sync2Server { get; set; }
    }

    public partial class RelayAskResultInfo
    {
        public ulong FlowingId { get; set; }

        public List<RelayServerNodeReportInfo> Nodes { get; set; } = new List<RelayServerNodeReportInfo>();
    }
    public partial class RelayAskResultInfo170: RelayAskResultInfo
    {
        public new List<RelayServerNodeReportInfo170> Nodes { get; set; } = new List<RelayServerNodeReportInfo170>();
    }
}
