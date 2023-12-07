using cmonitor.service;
using cmonitor.service.messengers.sign;
using cmonitor.service.messengers.wallpaper;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class WallpaperClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public WallpaperClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public bool Update(ClientServiceParamsInfo param)
        {
            WallpaperLockInfo info = param.Content.DeJson<WallpaperLockInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(new WallpaperUpdateInfo
            {
                Value = info.Value,
                Url = info.Url
            });
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)WallpaperMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }
            return true;
        }
    }

    public sealed class WallpaperLockInfo
    {
        public string[] Names { get; set; }
        public bool Value { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
