using common.libs;

namespace cmonitor.server.client.reports.llock
{
    public sealed class WallpaperReport : IReport
    {
        public string Name => "Wallpaper";

        private Dictionary<string, object> report = new Dictionary<string, object>() { { "Value", false } };
        private readonly Config config;
        public WallpaperReport(Config config)
        {
            this.config = config;
        }

        public Dictionary<string, object> GetReports()
        {
            report["Value"] = WindowHelper.GetHasWindowByName("wallpaper.win");
            return report;
        }

        public void Update(bool open, string url)
        {
            if (open)
            {
                Task.Run(() =>
                {
                    CommandHelper.Windows(string.Empty, new string[] {
                        $"start wallpaper.win.exe \"{url}\"  {config.KeyboardMemoryKey} {config.KeyboardMemoryLength}"
                    });
                });
            }
            else
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        "taskkill /f /t /im \"wallpaper.win.exe\""
                    });
            }
        }
    }
}
