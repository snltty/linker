using cmonitor.config;
using common.libs;

namespace cmonitor.plugins.wallpaper.report
{
    public sealed class WallpaperWindows : IWallpaper
    {
        private readonly Config config;
        public WallpaperWindows(Config config)
        {
            this.config = config;
        }
        public void Set(WallpaperConfigInfo info)
        {
            if (info.Open)
            {
                /*
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                string file = Path.Combine(dir, "./plugins/wallpaper/bg.jpg");
                User32.SystemParametersInfo(User32.SPI_SETDESKWALLPAPER, 0, file, User32.SPIF_UPDATEINIFILE | User32.SPIF_SENDCHANGE);
                */

                CommandHelper.Windows(string.Empty, new string[] {
                        $"start ./plugins/wallpaper/cmonitor.wallpaper.win.exe \"{info.ImgUrl}\" {config.Data.Client.ShareMemoryKey} {config.Data.Client.ShareMemoryCount} {config.Data.Client.ShareMemorySize} {(int)ShareMemoryIndexs.Keyboard} {(int)ShareMemoryIndexs.Wallpaper}"
                    },false);
            }
        }
    }
}
