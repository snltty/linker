using cmonitor.config;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.setting.messenger
{
    public sealed class SettingClientMessenger : IMessenger
    {
        private Config config;
        public SettingClientMessenger(Config config)
        {
            this.config = config;
        }

        [MessengerId((ushort)SettingMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            SettingInfo settingInfo = MemoryPackSerializer.Deserialize<SettingInfo>(connection.ReceiveRequestWrap.Payload.Span);
            
        }

    }
    [MemoryPackable]
    public sealed partial class SettingShareInfo
    {
        public float ScreenScale { get; set; } = 0.2f;
        public int ScreenDelay { get; set; } = 30;

    }

    [MemoryPackable]
    public sealed partial class SettingInfo
    {
        public int ReportDelay { get; set; } = 30;
        public float ScreenScale { get; set; } = 0.2f;
        public int ScreenDelay { get; set; } = 30;
        public bool SaveSetting { get; set; } = true;
        public bool WakeUp { get; set; } = true;
        public bool VolumeMasterPeak { get; set; } = true;
        
    }
}
