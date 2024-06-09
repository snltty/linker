using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using common.libs;
using cmonitor.client.config;

namespace cmonitor.plugins.wallpaper.report
{
    public sealed class WallpaperReport : IClientReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private readonly RunningConfig  runningConfig;
        private readonly IWallpaper wallpaper;
        private readonly ShareMemory shareMemory;

        public WallpaperReport(Config config, RunningConfig runningConfig, IWallpaper wallpaper, ShareMemory shareMemory, ClientSignInState clientSignInState)
        {
            this.runningConfig = runningConfig;
            this.wallpaper = wallpaper;
            this.shareMemory = shareMemory;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Update(runningConfig.Data.Wallpaper);
                WallpaperTask();
            };
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = Running();
            if (reportType == ReportType.Full || report.Updated() || shareMemory.ReadVersionUpdated((int)ShareMemoryIndexs.Wallpaper))
            {
                return report;
            }
            return null;
        }

        public void Update(WallpaperConfigInfo info)
        {
            runningConfig.Data.Wallpaper = info;
            Task.Run(async () =>
            {
                shareMemory.AddAttribute((int)ShareMemoryIndexs.Wallpaper, ShareMemoryAttribute.Closed);
                await Task.Delay(100);
                wallpaper.Set(runningConfig.Data.Wallpaper);
            });
        }

        private bool Running()
        {
            long version = shareMemory.ReadVersion((int)ShareMemoryIndexs.Wallpaper);
            return shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.Wallpaper, ShareMemoryAttribute.Running)
                    && Helper.Timestamp() - version < 1000;
        }
        private void WallpaperTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (runningConfig.Data.Wallpaper.Open)
                    {
                        if (Running() == false)
                        {
                            Update(runningConfig.Data.Wallpaper);
                        }
                    }
                    await Task.Delay(5000);
                }
            });
        }
    }

    public sealed class WallpaperReportInfo:ReportInfo
    {
        public bool Value { get; set; }

        public override int HashCode()
        {
            return Value.GetHashCode();
        }
    }
}
