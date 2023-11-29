using cmonitor.server.client.reports.wallpaper;
using MemoryPack;

namespace cmonitor.server.service.messengers.wallpaper
{
    public sealed class WallpaperMessenger : IMessenger
    {
        private readonly WallpaperReport wallpaperReport;

        public WallpaperMessenger(WallpaperReport wallpaperReport)
        {
            this.wallpaperReport = wallpaperReport;
        }

        [MessengerId((ushort)WallpaperMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            WallpaperUpdateInfo wallpaperUpdateInfo = MemoryPackSerializer.Deserialize<WallpaperUpdateInfo>(connection.ReceiveRequestWrap.Payload.Span);
            wallpaperReport.Update(wallpaperUpdateInfo.Value, wallpaperUpdateInfo.Url);
        }
    }

    [MemoryPackable]
    public sealed partial class WallpaperUpdateInfo
    {
        public bool Value { get; set; }
        public string Url { get; set; }
    }

}
