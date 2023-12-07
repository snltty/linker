using cmonitor.client.reports.notify;
using cmonitor.service;
using cmonitor.service.messengers.notify;
using cmonitor.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class NotifyClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public NotifyClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            NotifyInfo info = param.Content.DeJson<NotifyInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info);
            foreach (SignCacheInfo cache in signCaching.Get())
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
