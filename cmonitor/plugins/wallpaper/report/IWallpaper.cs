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
