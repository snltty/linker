using cmonitor.api;
using cmonitor.config;
using cmonitor.plugins.setting.messenger;
using cmonitor.plugins.signIn.messenger;
using cmonitor.server;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.setting
{
    public sealed class SettingApiController : IApiController
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;

        public SettingApiController(MessengerSender messengerSender, SignCaching signCaching, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }
        public SettingInfo Get(ApiControllerParamsInfo param)
        {
            return new SettingInfo
            {
            };
        }

        public bool Set(ApiControllerParamsInfo param)
        {
            SettingInfo settingInfo = param.Content.DeJson<SettingInfo>();

            byte[] bytes = MemoryPackSerializer.Serialize(settingInfo);
            foreach (var item in signCaching.Get())
            {
                if (item.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)SettingMessengerIds.Update,
                        Payload = bytes
                    });
                }
            }
            return true;
        }
    }

}
