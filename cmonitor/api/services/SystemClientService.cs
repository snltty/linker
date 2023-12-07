using cmonitor.client.reports.system;
using cmonitor.service;
using cmonitor.service.messengers.sign;
using cmonitor.service.messengers.system;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class SystemClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public SystemClientService(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Password(ClientServiceParamsInfo param)
        {
            PasswordInfo info = param.Content.DeJson<PasswordInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Input);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SystemMessengerIds.Password,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

        public async Task<bool> RegistryOptions(ClientServiceParamsInfo param)
        {
            RegistryInfo info = param.Content.DeJson<RegistryInfo>();
            byte[] bytes = MemoryPackSerializer.Serialize(info.Registry);
            for (int i = 0; i < info.Names.Length; i++)
            {
                if (signCaching.Get(info.Names[i], out SignCacheInfo cache) && cache.Connected)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SystemMessengerIds.RegistryOptions,
                        Payload = bytes
                    });
                }
            }

            return true;
        }

    }

    public sealed class RegistryInfo
    {
        public string[] Names { get; set; }
        public SystemOptionUpdateInfo Registry { get; set; }
    }

    public sealed class PasswordInfo
    {
        public string[] Names { get; set; }
        public PasswordInputInfo Input { get; set; }
    }
}
