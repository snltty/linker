using cmonitor.server.client.reports.share;
using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class LLockReport : IReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();
        private readonly Config config;
        private readonly ShareReport shareReport;
        public LLockReport(Config config, ShareReport shareReport)
        {
            this.config = config;
            this.shareReport = shareReport;
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports()
        {
            if (shareReport.GetShare(Name, out ShareItemInfo share) && string.IsNullOrWhiteSpace(share.Value) == false && long.TryParse(share.Value, out long time))
            {
                report.Value = (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - time < 1000;
            }
            return report;
        }

        public void Update(bool open)
        {
            CommandHelper.Windows(string.Empty, new string[] { "taskkill /f /t /im \"llock.win.exe\"" });
            if (open)
            {
                Task.Run(() =>
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start llock.win.exe {config.ShareMemoryKey} {config.ShareMemoryLength} {Config.ShareMemoryLLockIndex}"
                    });
                });
            }
        }
    }

    public sealed class LLockReportInfo
    {
        public bool Value { get; set; }
    }
}

