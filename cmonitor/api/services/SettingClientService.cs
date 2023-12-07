using cmonitor.service;
using cmonitor.service.messengers.setting;
using cmonitor.service.messengers.sign;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.api.services
{
    public sealed class SettingClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;
        private readonly Config config;

        public SettingClientService(MessengerSender messengerSender, SignCaching signCaching, Config config)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.config = config;
        }
        public SettingInfo Get(ClientServiceParamsInfo param)
        {
            return new SettingInfo
            {
                ReportDelay = config.ReportDelay,
                ScreenDelay = config.ScreenDelay,
                ScreenScale = config.ScreenScale,
                SaveSetting = config.SaveSetting,
                WakeUp = config.WakeUp,
                VolumeMasterPeak = config.VolumeMasterPeak,
            };
        }

        public bool Set(ClientServiceParamsInfo param)
        {
            SettingInfo settingInfo = param.Content.DeJson<SettingInfo>();
            config.ReportDelay = settingInfo.ReportDelay;
            config.ScreenDelay = settingInfo.ScreenDelay;
            config.ScreenScale = settingInfo.ScreenScale;
            config.SaveSetting = settingInfo.SaveSetting;
            config.WakeUp = settingInfo.WakeUp;
            config.VolumeMasterPeak = settingInfo.VolumeMasterPeak;

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
