using linker.messenger.updater;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigCommonInfo
    {
        public string UpdateUrl { get; set; } = "https://static.qbcode.cn/downloads/linker";
        public int UpdateIntervalSeconds { get; set; } = 60;
    }
    public partial class ConfigClientInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public UpdaterConfigClientInfo Updater { get; set; } = new UpdaterConfigClientInfo();
    }
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public UpdaterConfigServerInfo Updater { get; set; } = new UpdaterConfigServerInfo();
    }
}
