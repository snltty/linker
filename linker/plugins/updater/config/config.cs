using linker.libs;
using MemoryPack;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 自动更新密钥
        /// </summary>
        public string UpdaterSecretKey { get; set; } = Helper.GlobalString;
    }

}



namespace linker.config
{
    public sealed partial class ConfigCommonInfo
    {
        public string UpdateUrl { get; set; } = "https://static.qbcode.cn/downloads/linker";
    }
}

namespace linker.plugins.updater.config
{
    [MemoryPackable]
    public sealed partial class UpdaterConfirmInfo
    {
        public string MachineId { get; set; }
        public string Version { get; set; }
        public string SecretKey { get; set; }
        public bool GroupAll { get; set; }
        public bool All { get; set; }
    }

    [MemoryPackable]
    public sealed partial class UpdaterConfirmServerInfo
    {
        public string SecretKey { get; set; }
        public string Version { get; set; }
    }
}


namespace linker.config
{
    public partial class ConfigClientInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public UpdaterConfigClientInfo Updater { get; set; } = new UpdaterConfigClientInfo();
    }

    public sealed class UpdaterConfigClientInfo
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = Helper.GlobalString;
    }

    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public UpdaterConfigServerInfo Updater { get; set; } = new UpdaterConfigServerInfo();
    }

    public sealed class UpdaterConfigServerInfo
    {
        /// <summary>
        /// 密钥
        /// </summary>
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }


}