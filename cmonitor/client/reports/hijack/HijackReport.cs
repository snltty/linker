using common.libs;
using MemoryPack;

namespace cmonitor.client.reports.hijack
{
    public sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackConfig hijackConfig;
        private readonly ClientConfig clientConfig;
        private readonly IHijack hijack;
        private readonly ClientSignInState clientSignInState;

        private HijackReportInfo hijackReportInfo = new HijackReportInfo();
        private int hashCode = 0;
        private long ticks = DateTime.UtcNow.Ticks;

        public HijackReport(IHijack hijack, HijackConfig hijackConfig, ClientConfig clientConfig, Config config, ClientSignInState clientSignInState)
        {
            this.hijack = hijack;
            this.hijackConfig = hijackConfig;
            this.clientConfig = clientConfig;
            this.clientSignInState = clientSignInState;
            if (config.IsCLient)
            {
                Init();
            }
        }

        private void Init()
        {
            try
            {
                clientSignInState.NetworkFirstEnabledHandle += hijack.Start;

                hijackConfig.DeniedProcesss = clientConfig.HijackConfig.DeniedProcesss;
                hijackConfig.AllowProcesss = clientConfig.HijackConfig.AllowProcesss;
                hijackConfig.DeniedDomains = clientConfig.HijackConfig.DeniedDomains;
                hijackConfig.AllowDomains = clientConfig.HijackConfig.AllowDomains;
                hijackConfig.DeniedIPs = clientConfig.HijackConfig.DeniedIPs;
                hijackConfig.AllowIPs = clientConfig.HijackConfig.AllowIPs;
                hijackConfig.DomainKill = clientConfig.HijackConfig.DomainKill;
                UpdateRules();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public void Update(HijackSetRuleInfo info)
        {
            hijackConfig.AllowDomains = info.Rules.AllowDomains;
            hijackConfig.DeniedDomains = info.Rules.DeniedDomains;
            hijackConfig.AllowProcesss = info.Rules.AllowProcesss;
            hijackConfig.DeniedProcesss = info.Rules.DeniedProcesss;
            hijackConfig.AllowIPs = info.Rules.AllowIPs;
            hijackConfig.DeniedIPs = info.Rules.DeniedIPs;
            hijackConfig.DomainKill = info.Rules.DomainKill;

            clientConfig.HijackConfig = hijackConfig;
            clientConfig.HijackIds = info.Ids;

            UpdateRules();
        }

        private void UpdateRules()
        {
            hijack.SetProcess(hijackConfig.AllowProcesss, hijackConfig.DeniedProcesss);
            hijack.SetDomain(hijackConfig.AllowDomains, hijackConfig.DeniedDomains, hijackConfig.DomainKill);
            hijack.SetIP(hijackConfig.AllowIPs, hijackConfig.DeniedIPs);
            hijack.UpdateRules();
        }

        public object GetReports(ReportType reportType)
        {
            hijackReportInfo.Upload = hijack.UdpSend + hijack.TcpSend;
            hijackReportInfo.Download = hijack.TcpReceive + hijack.UdpReceive;
            hijackReportInfo.Count = (ulong)(hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length + hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length + hijackConfig.DeniedProcesss.Length);
            hijackReportInfo.Ids = clientConfig.HijackIds;
            hijackReportInfo.DomainKill = clientConfig.HijackConfig.DomainKill;

            long _ticks = DateTime.UtcNow.Ticks;
            int hashcode = hijackReportInfo.HashCode();
            if (((_ticks - ticks) / TimeSpan.TicksPerMillisecond >= 300 && hashCode != hashcode) || reportType == ReportType.Full)
            {
                ticks = _ticks;
                hashCode = hashcode;
                return hijackReportInfo;
            }
            return null;
        }
    }

    public sealed class HijackReportInfo
    {
        public ulong Upload { get; set; }
        public ulong Download { get; set; }
        public ulong Count { get; set; }

        public uint[] Ids { get; set; }
        public bool DomainKill { get; set; }

        public int HashCode()
        {
            return Upload.GetHashCode() ^ Download.GetHashCode() ^ Count.GetHashCode() ^ Ids.GetHashCode();
        }

    }

    [MemoryPackable]
    public sealed partial class HijackSetRuleInfo
    {
        public HijackRuleUpdateInfo Rules { get; set; }
        public uint[] Ids { get; set; }
    }


    [MemoryPackable]
    public sealed partial class HijackRuleUpdateInfo
    {
        /// <summary>
        /// 进程白名单
        /// </summary>
        public string[] AllowProcesss { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 进程黑名单
        /// </summary>
        public string[] DeniedProcesss { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 域名白名单
        /// </summary>
        public string[] AllowDomains { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 域名黑名单
        /// </summary>
        public string[] DeniedDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// ip白名单
        /// </summary>
        public string[] AllowIPs { get; set; } = Array.Empty<string>();
        /// <summary>
        /// ip黑名单
        /// </summary>
        public string[] DeniedIPs { get; set; } = Array.Empty<string>();

        public bool DomainKill { get; set; }
    }
}
