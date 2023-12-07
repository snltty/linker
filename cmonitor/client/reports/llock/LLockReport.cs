using cmonitor.client.reports.share;
using cmonitor.libs;

namespace cmonitor.client.reports.llock
{
    public sealed class LLockReport : IReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();

        private readonly ClientConfig clientConfig;
        private readonly ILLock lLock;
        private readonly ShareMemory shareMemory;

        public LLockReport(Config config, ClientConfig clientConfig, ILLock lLock, ShareMemory shareMemory)
        {
            this.clientConfig = clientConfig;
            this.lLock = lLock;
            this.shareMemory = shareMemory;

            if (config.IsCLient)
            {
                LockScreen(clientConfig.LLock);
            }
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            bool updated = shareMemory.ReadVersionUpdated(Config.ShareMemoryLLockIndex);


            if (reportType == ReportType.Full || updated)
            {
                long value = shareMemory.ReadValueInt64(Config.ShareMemoryLLockIndex);
                clientConfig.LLock = report.LockScreen = value > 0 && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - value < 1000;
                return report;
            }
            return null;
        }

        public void LockScreen(bool open)
        {
            clientConfig.LLock = open;
            Task.Run(async () =>
            {
                shareMemory.AddAttribute(Config.ShareMemoryLLockIndex, ShareMemoryAttribute.Closed);
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

