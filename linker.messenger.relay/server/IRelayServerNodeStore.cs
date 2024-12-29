using linker.libs;
using System.Net;
using linker.libs.extends;

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
        /// 设置月份
        /// </summary>
        /// <param name="month"></param>
        public void SetMaxGbTotalMonth(int month);
        /// <summary>
        /// 设置剩余流量
        /// </summary>
        /// <param name="value"></param>
        public void SetMaxGbTotalLastBytes(ulong value);
        /// <summary>
        /// 提交保存
        /// </summary>
        public void Confirm();
    }


    public sealed class RelayServerNodeInfo
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
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public ulong MaxGbTotalLastBytes { get; set; }
        public int MaxGbTotalMonth { get; set; }

        public bool Public { get; set; }

        public string MasterHost { get; set; } = string.Empty;
#if DEBUG
        public string MasterSecretKey { get; set; } = Helper.GlobalString;
#else
        public string MasterSecretKey { get; set; } = string.Empty;
#endif
    }

    public sealed partial class RelayServerNodeReportInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int MaxConnection { get; set; }
        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public ulong MaxGbTotalLastBytes { get; set; }

        public double ConnectionRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public bool Public { get; set; }

        public int Delay { get; set; }

        public IPEndPoint EndPoint { get; set; }

        public long LastTicks { get; set; }
    }


    public sealed partial class RelayAskResultInfo
    {
        public ulong FlowingId { get; set; }

        public List<RelayServerNodeReportInfo> Nodes { get; set; } = new List<RelayServerNodeReportInfo>();
    }

}
