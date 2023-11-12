using cmonitor.server.client.reports.share;
using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class WallpaperReport : IReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private bool lastValue;
        private readonly Config config;
        private readonly ShareReport shareReport;
        private readonly ClientConfig clientConfig;

        public WallpaperReport(Config config, ShareReport shareReport, ClientConfig clientConfig)
        {
            this.config = config;
            this.shareReport = shareReport;
            this.clientConfig = clientConfig;

            Update(clientConfig.Wallpaper, clientConfig.WallpaperUrl);
        }

        DateTime startTime = new DateTime(1970, 1, 1);
        public object GetReports(ReportType reportType)
        {
            clientConfig.Wallpaper = report.Value = shareReport.GetShare(Name, out ShareItemInfo share)
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
                shareReport.Update(new ShareItemInfo
                {
                    Index = Config.ShareMemoryWallpaperIndex,
                    Value = "close"
                });
                //  CommandHelper.Windows(string.Empty, new string[] { "taskkill /f /t /im \"wallpaper.win.exe\"" });
                await Task.Delay(100);

                if (open)
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start wallpaper.win.exe \"{url}\" {config.ShareMemoryKey} {config.ShareMemoryLength* Config.ShareMemoryItemLength} {Config.ShareMemoryKeyBoardIndex} {Config.ShareMemoryWallpaperIndex}"
                    });
                }
            });

        }
    }

    public sealed class WallpaperReportInfo
    {
        public bool Value { get; set; }
    }
}
