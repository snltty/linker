using cmonitor.api;
using cmonitor.plugins.signIn.messenger;
using cmonitor.plugins.system.messenger;
using cmonitor.plugins.system.report;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.system
{
    public sealed class SystemApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        public SystemApiController(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }
        public async Task<bool> Password(ApiControllerParamsInfo param)
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

        public async Task<bool> RegistryOptions(ApiControllerParamsInfo param)
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
