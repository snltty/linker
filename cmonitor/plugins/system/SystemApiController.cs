using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.system.messenger;
using cmonitor.plugins.system.report;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;
using common.libs.api;
using cmonitor.plugins.sapi;

namespace cmonitor.plugins.system
{
    public sealed class SystemApiController : IApiServerController
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
            for (int i = 0; i < info.Devices.Length; i++)
            {
                if (signCaching.Get(info.Devices[i], out SignCacheInfo cache) && cache.Connected)
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
            byte[] bytes = MemoryPackSerializer.Serialize(info.Data);
            for (int i = 0; i < info.Devices.Length; i++)
            {
                if (signCaching.Get(info.Devices[i], out SignCacheInfo cache) && cache.Connected)
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
        public string[] Devices { get; set; }
        public SystemOptionUpdateInfo[] Data { get; set; }
    }

    public sealed class PasswordInfo
    {
        public string[] Devices { get; set; }
        public PasswordInputInfo Input { get; set; }
    }
}
