using cmonitor.server.service;
using cmonitor.server.service.messengers.light;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class LightClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public LightClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }


        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            LightInfo info = param.Content.DeJson<LightInfo>();
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)LightMessengerIds.Update,
                        Payload = MemoryPackSerializer.Serialize(info.Value)
                    });
                }
            }

            return true;
        }

    }

    public sealed class LightInfo
    {
        public string[] Names { get; set; }
        public int Value { get; set; }
    }
}
