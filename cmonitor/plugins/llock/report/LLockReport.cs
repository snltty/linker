using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using common.libs;
using cmonitor.plugins.llock.report;
using cmonitor.client.config;
using LiteDB;

namespace cmonitor.plugins.llock.report
{
    public sealed class LLockReport : IClientReport
    {
        public string Name => "LLock";

        private LLockReportInfo report = new LLockReportInfo();

        private readonly RunningConfig runningConfig;
        private readonly ILLock lLock;
        private readonly ShareMemory shareMemory;

        public LLockReport(Config config, RunningConfig runningConfig, ILLock lLock, ShareMemory shareMemory, ClientSignInState clientSignInState)
        {
            this.runningConfig = runningConfig;
            this.lLock = lLock;
            this.shareMemory = shareMemory;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                LockScreen(runningConfig.Data.LLock.Open);
                WallpaperTask();
            };
        }

        public object GetReports(ReportType reportType)
        {
            report.LockScreen = Running();
            if (reportType == ReportType.Full || report.Updated() || shareMemory.ReadVersionUpdated((int)ShareMemoryIndexs.LLock))
            {
                return report;
            }
            return null;
        }

        public void LockScreen(bool open)
        {
            runningConfig.Data.LLock.Open = open;
            runningConfig.Data.Update();
            Task.Run(async () =>
            {
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
                    if (runningConfig.Data.LLock.Open)
                    {
                        if (Running() == false)
                        {
                            LockScreen(runningConfig.Data.LLock.Open);
                        }
                    }
                    await Task.Delay(5000);
                }
            });
        }

    }

    public sealed class LLockConfigInfo
    {
        public ObjectId Id { get; set; }
        public bool Open { get; set; }
    }

    public sealed class LLockReportInfo : ReportInfo
    {
        public bool LockScreen { get; set; }

        public override int HashCode()
        {
            return LockScreen.GetHashCode();
        }
    }
}


namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        private LLockConfigInfo llock = new LLockConfigInfo();
        public LLockConfigInfo LLock
        {
            get => llock; set
            {
                Updated++;
                llock = value;
            }
        }
    }
}


