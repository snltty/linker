using cmonitor.plugins.wallpaper.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.wallpaper.messenger
{
    public sealed class WallpaperClientMessenger : IMessenger
    {
        private readonly WallpaperReport wallpaperReport;

        public WallpaperClientMessenger(WallpaperReport wallpaperReport)
        {
            this.wallpaperReport = wallpaperReport;
        }

        [MessengerId((ushort)WallpaperMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            WallpaperConfigInfo wallpaperUpdateInfo = MemoryPackSerializer.Deserialize<WallpaperConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            wallpaperReport.Update(wallpaperUpdateInfo);
        }
    }

}
