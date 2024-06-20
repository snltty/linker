using cmonitor.plugins.notify.messenger;
using cmonitor.plugins.notify.report;
using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.server.sapi;

namespace cmonitor.plugins.notify
{
    public sealed class NotifyApiController : IApiServerController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public NotifyApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            NotifyInfo info = param.Content.DeJson<NotifyInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info);
            foreach (SignCacheInfo cache in signCaching.Get(info.GroupId))
            {
                if (cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)NotifyMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }
            return true;
        }
    }
}
