using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.volume.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.sapi;

namespace cmonitor.plugins.volume
{
    public sealed class VolumeApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public VolumeApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            VolumeInfo info = param.Content.DeJson<VolumeInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)VolumeMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

        public async Task<bool> Mute(ApiControllerParamsInfo param)
        {
            VolumeMuteInfo info = param.Content.DeJson<VolumeMuteInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)VolumeMessengerIds.Mute,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

        public async Task<bool> Play(ApiControllerParamsInfo param)
        {
            VolumePlayInfo info = param.Content.DeJson<VolumePlayInfo>();

            byte[] bytes = MemoryPackSerializer.Serialize(info.Base64);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)VolumeMessengerIds.Play,
                        Payload = bytes
                    });
                }
            }
            return true;
        }
    }

    public sealed class VolumeInfo
    {
        public string[] Names { get; set; }
        public float Value { get; set; }
    }
    public sealed class VolumeMuteInfo
    {
        public string[] Names { get; set; }
        public bool Value { get; set; }
    }

    public sealed class VolumePlayInfo
    {
        public string[] Names { get; set; }
        public string Base64 { get; set; }
    }
}
