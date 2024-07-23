using MemoryPack;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 自动更新密钥
        /// </summary>
        public string UpdaterSecretKey { get; set; } = "snltty";
    }

}


namespace linker.plugins.updater.config
{
    [MemoryPackable]
    public sealed partial class UpdaterConfirmInfo
    {
        public string MachineId { get; set; }
        public string Version { get; set; }
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
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
    }


}