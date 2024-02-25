using cmonitor.libs;

namespace cmonitor.client.reports.wallpaper
{
    public sealed class WallpaperReport : IReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private readonly ClientConfig clientConfig;
        private readonly IWallpaper wallpaper;
        private readonly ShareMemory shareMemory;
        private long version = 0;
        private bool opened = false;

        public WallpaperReport(Config config, ClientConfig clientConfig, IWallpaper wallpaper, ShareMemory shareMemory, ClientSignInState clientSignInState)
        {
            this.clientConfig = clientConfig;
            this.wallpaper = wallpaper;
            this.shareMemory = shareMemory;

            if (config.IsCLient)
            {
                clientSignInState.NetworkFirstEnabledHandle += () => { Update(clientConfig.Wallpaper, clientConfig.WallpaperUrl); };
                WallpaperTask();
            }
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            if (reportType == ReportType.Full || shareMemory.ReadVersionUpdated(Config.ShareMemoryWallpaperIndex, ref version))
            {
                report.Value = Running();
                return report;
            }
            return null;
        }

        public void Update(bool open, string url)
        {
            opened = open;
            clientConfig.Wallpaper = open;
            clientConfig.WallpaperUrl = url;
            Task.Run(async () =>
            {
                shareMemory.AddAttribute(Config.ShareMemoryWallpaperIndex, ShareMemoryAttribute.Closed);
                await Task.Delay(100);
                wallpaper.Set(open, url);
            });
        }

        private void WallpaperTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (opened)
                    {
                        if (Running() == false)
                        {
                            Update(clientConfig.Wallpaper, clientConfig.WallpaperUrl);
                        }
                    }
                    await Task.Delay(5000);
                }
            });
        }
        private bool Running()
        {
            long value = shareMemory.ReadValueInt64(Config.ShareMemoryWallpaperIndex);
            return value > 0 && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - value < 1000;
        }
    }

    public sealed class WallpaperReportInfo
    {
        public bool Value { get; set; }
    }
}
