using cmonitor.service;
using cmonitor.service.messengers.command;
using cmonitor.service.messengers.llock;
using cmonitor.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
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
        public async Task<bool> LockScreen(ClientServiceParamsInfo param)
        {
            LLockUpdateInfo info = param.Content.DeJson<LLockUpdateInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Value);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)LLockMessengerIds.LockScreen,
                        Payload = bytes
                    });
                }
            }
            return true;
        }


        public async Task<bool> LockSystem(ClientServiceParamsInfo param)
        {
            string[] names = param.Content.DeJson<string[]>();
            for (int i = 0; i < names.Length; i++)
            {
                if (signCaching.Get(names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)LLockMessengerIds.LockSystem
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

    public sealed class LLockAutoLockSystemInfo
    {
        public string[] Names { get; set; }
        public bool Value { get; set; }
    }
}
