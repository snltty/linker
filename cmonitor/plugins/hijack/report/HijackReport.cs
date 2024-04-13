using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using common.libs;
using MemoryPack;
using cmonitor.client.running;

namespace cmonitor.plugins.hijack.report
{
    public sealed class HijackReport : IClientReport
    {
        public string Name => "Hijack";

        private readonly RunningConfig runningConfig;
        private readonly IHijack hijack;
        private readonly ClientSignInState clientSignInState;

        private HijackReportInfo hijackReportInfo = new HijackReportInfo();
        private long ticks = DateTime.UtcNow.Ticks;

        public HijackReport(IHijack hijack, RunningConfig runningConfig, Config config, ClientSignInState clientSignInState)
        {
            this.hijack = hijack;
            this.runningConfig = runningConfig;
            this.clientSignInState = clientSignInState;
            Init();
        }

        private void Init()
        {
            try
            {
                clientSignInState.NetworkFirstEnabledHandle += hijack.Start;
                UpdateRules();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public void Update(HijackSetRuleInfo info)
        {
            runningConfig.Data.Hijack.AllowDomains = info.Rules.AllowDomains;
            runningConfig.Data.Hijack.DeniedDomains = info.Rules.DeniedDomains;
            runningConfig.Data.Hijack.AllowProcesss = info.Rules.AllowProcesss;
            runningConfig.Data.Hijack.DeniedProcesss = info.Rules.DeniedProcesss;
            runningConfig.Data.Hijack.AllowIPs = info.Rules.AllowIPs;
            runningConfig.Data.Hijack.DeniedIPs = info.Rules.DeniedIPs;
            runningConfig.Data.Hijack.DomainKill = info.Rules.DomainKill;
            runningConfig.Data.Hijack.HijackIds1 = info.Ids1;
            runningConfig.Data.Hijack.HijackIds2 = info.Ids2;

            runningConfig.Data.Update();

            UpdateRules();
        }

        private void UpdateRules()
        {
            hijack.SetProcess(runningConfig.Data.Hijack.AllowProcesss, runningConfig.Data.Hijack.DeniedProcesss);
            hijack.SetDomain(runningConfig.Data.Hijack.AllowDomains, runningConfig.Data.Hijack.DeniedDomains, runningConfig.Data.Hijack.DomainKill);
            hijack.SetIP(runningConfig.Data.Hijack.AllowIPs, runningConfig.Data.Hijack.DeniedIPs);
            hijack.UpdateRules();
        }

        public object GetReports(ReportType reportType)
        {
            hijackReportInfo.Upload = hijack.UdpSend + hijack.TcpSend;
            hijackReportInfo.Download = hijack.TcpReceive + hijack.UdpReceive;
            hijackReportInfo.Count = (ulong)(runningConfig.Data.Hijack.AllowIPs.Length + runningConfig.Data.Hijack.DeniedIPs.Length + runningConfig.Data.Hijack.AllowDomains.Length + runningConfig.Data.Hijack.DeniedDomains.Length + runningConfig.Data.Hijack.AllowProcesss.Length + runningConfig.Data.Hijack.DeniedProcesss.Length);
            hijackReportInfo.Ids1 = runningConfig.Data.Hijack.HijackIds1;
            hijackReportInfo.Ids2 = runningConfig.Data.Hijack.HijackIds2;
            hijackReportInfo.DomainKill = runningConfig.Data.Hijack.DomainKill;

            long _ticks = DateTime.UtcNow.Ticks;
            if (((_ticks - ticks) / TimeSpan.TicksPerMillisecond >= 300 && hijackReportInfo.Updated()) || reportType == ReportType.Full)
            {
                ticks = _ticks;
                return hijackReportInfo;
            }
            return null;
        }
    }

    public sealed class HijackReportInfo : ReportInfo
    {
        public ulong Upload { get; set; }
        public ulong Download { get; set; }
        public ulong Count { get; set; }

        public string[] Ids1 { get; set; }
        public string[] Ids2 { get; set; }
        public bool DomainKill { get; set; }

        public override int HashCode()
        {
            return Upload.GetHashCode() ^ Download.GetHashCode() ^ Count.GetHashCode() ^ Ids1.GetHashCode() ^ Ids2.GetHashCode();
        }

    }

    [MemoryPackable]
    public sealed partial class HijackSetRuleInfo
    {
        public HijackRuleUpdateInfo Rules { get; set; }
        public string[] Ids1 { get; set; }
        public string[] Ids2 { get; set; }
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
