using cmonitor.server.service;
using cmonitor.server.service.messengers.llock;
using cmonitor.server.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.server.api.services
{
    public sealed class LLockClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public LLockClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Update(ClientServiceParamsInfo param)
        {
            LLockUpdateInfo info = param.Content.DeJson<LLockUpdateInfo>();
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)LLockMessengerIds.Update,
                        Payload = MemoryPackSerializer.Serialize(info.Value)
                    });
                }
            }
            return true;
        }
    }
    public sealed class LLockUpdateInfo
    {
        public string[] Names { get; set; }
        public bool Value { get; set; }
    }
}
