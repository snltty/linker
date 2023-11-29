using cmonitor.server.client.reports.share;

namespace cmonitor.server.client.reports.wallpaper
{
    public sealed class WallpaperReport : IReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private bool lastValue;
        private readonly ShareReport shareReport;
        private readonly ClientConfig clientConfig;
        private readonly IWallpaper wallpaper;

        public WallpaperReport(ShareReport shareReport, ClientConfig clientConfig, IWallpaper wallpaper)
        {
            this.shareReport = shareReport;
            this.clientConfig = clientConfig;
            this.wallpaper = wallpaper;

            Update(clientConfig.Wallpaper, clientConfig.WallpaperUrl);
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            clientConfig.Wallpaper = report.Value = shareReport.GetShare(Name, out libs.ShareItemInfo share)
                && string.IsNullOrWhiteSpace(share.Value) == false
                && long.TryParse(share.Value, out long time) && (long)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds - time < 1000;

            if (reportType == ReportType.Full || report.Value != lastValue)
            {
                lastValue = report.Value;
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
                shareReport.WriteClose(Config.ShareMemoryWallpaperIndex);
                await Task.Delay(100);
                wallpaper.Set(open,url);
            });

        }
    }

    public sealed class WallpaperReportInfo
    {
        public bool Value { get; set; }
    }
}
