using cmonitor.plugins.wallpaper.report;
using MemoryPack;

namespace cmonitor.plugins.wallpaper.report
{
    public interface IWallpaper
    {
        public void Set(WallpaperConfigInfo info);
    }

    [MemoryPackable]
    public sealed partial class WallpaperConfigInfo
    {
        public bool Open { get; set; }
        public string ImgUrl { get; set; }
    }
}

namespace cmonitor.client.running
{
    public sealed partial class RunningConfigInfo
    {
        private WallpaperConfigInfo wallpaper = new WallpaperConfigInfo();
        public WallpaperConfigInfo Wallpaper
        {
            get => wallpaper; set
            {
                Updated++;
                wallpaper = value;
            }
        }
    }
}