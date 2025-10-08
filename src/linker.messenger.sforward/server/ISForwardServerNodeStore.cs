using linker.libs;
using System.Net;
using linker.libs.extends;
using System.Text.Json.Serialization;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透节点存储器
    /// </summary>
    public interface ISForwardServerNodeStore
    {
        /// <summary>
        /// 节点信息
        /// </summary>
        public SForwardServerNodeInfo Node { get; }

        public int ServicePort { get; }

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="node"></param>
        public void SetInfo(SForwardServerNodeInfo node);
        public void UpdateInfo(SForwardServerNodeUpdateInfo update);
        public void SetMasterHosts(string[] hosts);

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
        /// <summary>
        /// 提交保存
        /// </summary>
        public void Confirm();
    }


    public sealed class SForwardServerNodeInfo
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
        public string Domain { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;

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
        public bool Sync2Server { get; set; }

        public string[] MasterHosts { get; set; } = [];
    }

    public partial class SForwardServerNodeUpdateWrapInfo
    {
        public SForwardServerNodeUpdateInfo Info { get; set; }
    }
    public partial class SForwardServerNodeUpdateInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double MaxBandwidth { get; set; }
        public double MaxBandwidthTotal { get; set; }
        public double MaxGbTotal { get; set; }
        public long MaxGbTotalLastBytes { get; set; }
        public bool Public { get; set; }
        public string Domain { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Url { get; set; } = "https://linker-doc.snltty.com";
        public bool Sync2Server { get; set; } = false;

        public int WebPort { get; set; }
        public int[] PortRange { get; set; } = [1025, 65535];
    }

    public partial class SForwardServerNodeReportInfo
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
