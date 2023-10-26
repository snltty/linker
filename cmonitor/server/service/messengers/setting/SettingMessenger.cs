using MemoryPack;

namespace cmonitor.server.service.messengers.setting
{
    public sealed class SettingMessenger : IMessenger
    {
        private Config config;
        public SettingMessenger(Config config)
        {
            this.config = config;
        }

        [MessengerId((ushort)SettingMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            SettingInfo settingInfo = MemoryPackSerializer.Deserialize<SettingInfo>(connection.ReceiveRequestWrap.Payload.Span);
            config.ReportDelay = settingInfo.ReportDelay;
            config.ScreenDelay = settingInfo.ScreenDelay;
            config.ScreenScale = settingInfo.ScreenScale;
            config.SaveSetting = settingInfo.SaveSetting;
        }

    }

    [MemoryPackable]
    public sealed partial class SettingInfo
    {
        public int ReportDelay { get; set; } = 30;
        public float ScreenScale { get; set; } = 0.2f;
        public int ScreenDelay { get; set; } = 30;
        public bool SaveSetting { get; set; } = true;


    }
}
