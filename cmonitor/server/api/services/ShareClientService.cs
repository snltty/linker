using cmonitor.server.client.reports.share;
using cmonitor.server.service;
using cmonitor.server.service.messengers.share;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class ShareClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public ShareClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            ShareUpdateInfo info = param.Content.DeJson<ShareUpdateInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Item);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache))
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
