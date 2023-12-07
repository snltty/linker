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

        public WallpaperReport(ClientConfig clientConfig, IWallpaper wallpaper, ShareMemory shareMemory)
        {
            this.clientConfig = clientConfig;
            this.wallpaper = wallpaper;
            this.shareMemory = shareMemory;

            Update(clientConfig.Wallpaper, clientConfig.WallpaperUrl);
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            bool updated = shareMemory.ReadVersionUpdated(Config.ShareMemoryWallpaperIndex,ref version);
            if (reportType == ReportType.Full || updated)
            {
                long value = shareMemory.ReadValueInt64(Config.ShareMemoryWallpaperIndex);
                clientConfig.Wallpaper = report.Value = value > 0 && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - value < 1000;
                return report;
            }
            return null;
        }

        public void Update(bool open, string url)
        {
            clientConfig.Wallpaper = open;
            clientConfig.WallpaperUrl = url;
            Task.Run(async () =>
            {
                shareMemory.AddAttribute(Config.ShareMemoryWallpaperIndex, ShareMemoryAttribute.Closed);
                await Task.Delay(100);
                wallpaper.Set(open, url);
            });

        }
    }

    public sealed class WallpaperReportInfo
    {
        public bool Value { get; set; }
    }
}
