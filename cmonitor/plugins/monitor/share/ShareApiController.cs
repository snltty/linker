using cmonitor.plugins.share.messenger;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;

namespace cmonitor.plugins.share
{
    public sealed class ShareApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public ShareApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            ShareUpdateInfo info = param.Content.DeJson<ShareUpdateInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Item);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.TryGet(info.Names[i], out SignCacheInfo cache))
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ShareMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

    }

    public sealed class ShareUpdateInfo
    {
        public string[] Names { get; set; }
        public ShareItemInfo Item { get; set; }
    }
}
