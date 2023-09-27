using cmonitor.server.client.reports.share;
using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class WallpaperReport : IReport
    {
        public string Name => "Wallpaper";

        private WallpaperReportInfo report = new WallpaperReportInfo();
        private readonly Config config;
        private readonly ShareReport shareReport;
        public WallpaperReport(Config config, ShareReport shareReport)
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

        public void Update(bool open, string url)
        {
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] { "taskkill /f /t /im \"wallpaper.win.exe\"" });
                if (open)
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start wallpaper.win.exe \"{url}\" {config.ShareMemoryKey} {config.ShareMemoryLength} {Config.ShareMemoryKeyBoardIndex} {Config.ShareMemoryWallpaperIndex}"
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
