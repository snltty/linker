using cmonitor.server.client.reports.share;

namespace cmonitor.server.client.reports.llock
{
    public sealed class LLockReport : IReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();
        bool lastValue = false;

        private readonly ShareReport shareReport;
        private readonly ClientConfig clientConfig;
        private readonly ILLock lLock;

        public LLockReport(Config config, ShareReport shareReport, ClientConfig clientConfig, ILLock lLock)
        {
            this.shareReport = shareReport;
            this.clientConfig = clientConfig;
            this.lLock = lLock;

            if (config.IsCLient)
            {
                LockScreen(clientConfig.LLock);
            }
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            clientConfig.LLock = report.LockScreen = shareReport.GetShare(Name, out libs.ShareItemInfo share)
                && string.IsNullOrWhiteSpace(share.Value) == false
                && long.TryParse(share.Value, out long time) && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - time < 1000;

            if (reportType == ReportType.Full || report.LockScreen != lastValue)
            {
                lastValue = report.LockScreen;
                return report;
            }
            return null;
        }

        public void LockScreen(bool open)
        {
            clientConfig.LLock = open;
            Task.Run(async () =>
            {
                shareReport.WriteClose(Config.ShareMemoryLLockIndex);
                await Task.Delay(100);
                lLock.Set(open);
            });
        }

        public void LockSystem()
        {
            lLock.LockSystem();
        }

    }

    public sealed class LLockReportInfo
    {
        public bool LockScreen { get; set; }
    }
}

