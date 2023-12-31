using common.libs;

namespace cmonitor.client.reports.wallpaper
{
    public sealed class WallpaperWindows : IWallpaper
    {
        private readonly Config config;
        public WallpaperWindows(Config config)
        {
            this.config = config;
        }
        public void Set(bool value, string url)
        {
            if (value)
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.wallpaper.win.exe \"{url}\" {config.ShareMemoryKey} {config.ShareMemoryLength} {config.ShareMemoryItemSize} {Config.ShareMemoryKeyBoardIndex} {Config.ShareMemoryWallpaperIndex}"
                    });
            }
        }
    }
}
