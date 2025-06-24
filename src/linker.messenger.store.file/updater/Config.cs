using linker.messenger.updater;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigCommonInfo
    {
        public string UpdateUrl { get; set; } = "https://static.snltty.com/downloads/linker";
        public int UpdateIntervalSeconds { get; set; } = 60;
        public bool CheckUpdate { get; set; } = true;
    }
    public partial class ConfigClientInfo
    {
        public UpdaterConfigClientInfo Updater { get; set; } = new UpdaterConfigClientInfo();
    }
    public partial class ConfigServerInfo
    {
        public UpdaterConfigServerInfo Updater { get; set; } = new UpdaterConfigServerInfo();
    }
}
