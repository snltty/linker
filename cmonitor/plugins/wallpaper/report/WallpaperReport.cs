using cmonitor.client;
using cmonitor.client.runningConfig;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using common.libs;

namespace cmonitor.plugins.wallpaper.report
{
    public sealed class WallpaperReport : IClientReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private readonly IRunningConfig clientConfig;
        private readonly IWallpaper wallpaper;
        private readonly ShareMemory shareMemory;
        private WallpaperConfigInfo wallpaperConfig;

        public WallpaperReport(Config config, IRunningConfig clientConfig, IWallpaper wallpaper, ShareMemory shareMemory, ClientSignInState clientSignInState)
        {
            this.clientConfig = clientConfig;
            this.wallpaper = wallpaper;
            this.shareMemory = shareMemory;

            wallpaperConfig = clientConfig.Get(new WallpaperConfigInfo { });
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Update(wallpaperConfig);
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
            wallpaperConfig = info;
            Task.Run(async () =>
            {
                clientConfig.Set(wallpaperConfig);
                shareMemory.AddAttribute((int)ShareMemoryIndexs.Wallpaper, ShareMemoryAttribute.Closed);
                await Task.Delay(100);
                wallpaper.Set(wallpaperConfig);
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
                    if (wallpaperConfig.Open)
                    {
                        if (Running() == false)
                        {
                            Update(wallpaperConfig);
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
