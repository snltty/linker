using cmonitor.hijack;
using common.libs;

namespace cmonitor.server.client.reports.hijack
{
    internal sealed class HijackReport : IReport
    {
        public string Name => "Hijack";

        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackConfig hijackConfig;
        HijackReportInfo report = new HijackReportInfo();
        public HijackReport(HijackEventHandler hijackEventHandler, HijackController hijackController, HijackConfig hijackConfig)
        {
            this.hijackEventHandler = hijackEventHandler;
            this.hijackConfig = hijackConfig;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    hijackController.Start();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }

        public object GetReports()
        {
            report.Upload = hijackEventHandler.UdpSend + hijackEventHandler.TcpSend;
            report.Download = hijackEventHandler.TcpReceive + hijackEventHandler.UdpReceive;
            report.Count = hijackConfig.AllowIPs.Length + hijackConfig.DeniedIPs.Length + hijackConfig.AllowDomains.Length + hijackConfig.DeniedDomains.Length + hijackConfig.AllowProcesss.Length + hijackConfig.DeniedProcesss.Length;
            return report;
        }
    }

    public sealed class HijackReportInfo
    {
        public ulong Upload { get; set; }
        public ulong Download { get; set; }
        public int Count { get; set; }
    }
}
