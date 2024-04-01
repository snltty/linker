using cmonitor.client;
using cmonitor.client.runningConfig;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using common.libs;

namespace cmonitor.plugins.llock.report
{
    public sealed class LLockReport : IClientReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();

        private readonly IRunningConfig clientConfig;
        private readonly ILLock lLock;
        private readonly ShareMemory shareMemory;
        private readonly LLockConfigInfo lLockConfigInfo = new LLockConfigInfo();

        public LLockReport(Config config, IRunningConfig clientConfig, ILLock lLock, ShareMemory shareMemory, ClientSignInState clientSignInState)
        {
            this.clientConfig = clientConfig;
            this.lLock = lLock;
            this.shareMemory = shareMemory;

            lLockConfigInfo = clientConfig.Get(new LLockConfigInfo { });
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                LockScreen(lLockConfigInfo.Open);
                WallpaperTask();
            };
        }

        public object GetReports(ReportType reportType)
        {
            if (reportType == ReportType.Full || shareMemory.ReadVersionUpdated((int)ShareMemoryIndexs.LLock))
            {
                bool old = lLockConfigInfo.Open;
                lLockConfigInfo.Open = report.LockScreen = Running();
                if (lLockConfigInfo.Open != old)
                {
                    clientConfig.Set(lLockConfigInfo);
                }
                return report;
            }
            return null;
        }

        public void LockScreen(bool open)
        {
            lLockConfigInfo.Open = open;
            Task.Run(async () =>
            {
                clientConfig.Set(lLockConfigInfo);
                shareMemory.AddAttribute((int)ShareMemoryIndexs.LLock, ShareMemoryAttribute.Closed);
                await Task.Delay(100);
                lLock.Set(open);
            });
        }

        public void LockSystem()
        {
            lLock.LockSystem();
        }


        private bool Running()
        {
            long version = shareMemory.ReadVersion((int)ShareMemoryIndexs.LLock);
            return shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.LLock, ShareMemoryAttribute.Running)
                    && Helper.Timestamp() - version < 1000;
        }
        private void WallpaperTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (lLockConfigInfo.Open)
                    {
                        if (Running() == false)
                        {
                            LockScreen(lLockConfigInfo.Open);
                        }
                    }
                    await Task.Delay(5000);
                }
            });
        }

    }

    public sealed class LLockConfigInfo
    {
        public bool Open { get; set; }
    }

    public sealed class LLockReportInfo
    {
        public bool LockScreen { get; set; }
    }
}

