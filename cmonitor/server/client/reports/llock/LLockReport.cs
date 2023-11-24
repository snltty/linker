using cmonitor.server.client.reports.screen.winapiss;
using cmonitor.server.client.reports.share;
using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class LLockReport : IReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();
        bool lastValue = false;
        bool update = false;

        private readonly Config config;
        private readonly ShareReport shareReport;
        private readonly ClientConfig clientConfig;

        public LLockReport(Config config, ShareReport shareReport, ClientConfig clientConfig)
        {
            this.config = config;
            this.shareReport = shareReport;
            this.clientConfig = clientConfig;
            if (OperatingSystem.IsWindows() && config.IsCLient)
            {
                LockScreen(clientConfig.LLock);
            }
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            clientConfig.LLock = report.LockScreen = shareReport.GetShare(Name, out cmonitor.libs.ShareItemInfo share)
                && string.IsNullOrWhiteSpace(share.Value) == false
                && long.TryParse(share.Value, out long time) && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - time < 1000;

            if (reportType == ReportType.Full || update || report.LockScreen != lastValue)
            {
                update = false;
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
                if (open)
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start llock.win.exe {config.ShareMemoryKey} {config.ShareMemoryLength} {config.ShareMemoryItemLength} {Config.ShareMemoryLLockIndex}"
                    });
                }
            });
        }

        public void LockSystem()
        {
            User32.LockWorkStation();
        }

    }

    public enum EnableLock : byte
    {
        Disabled = 0,
        Enables = 1
    }

    public sealed class LLockReportInfo
    {
        public bool LockScreen { get; set; }
    }
}

