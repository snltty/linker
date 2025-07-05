namespace linker.messenger.store.file
{
    public sealed partial class ConfigClientInfo
    {
    }
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// cdkey配置
        /// </summary>
        public CdkeyConfigInfo Cdkey { get; set; } = new CdkeyConfigInfo();
    }
}
