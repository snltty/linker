using linker.messenger.cdkey;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigClientInfo
    {
        /// <summary>
        /// cdkey配置
        /// </summary>
        public CdkeyConfigInfo Cdkey { get; set; } = new CdkeyConfigInfo();
    }
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// cdkey配置
        /// </summary>
        public CdkeyConfigInfo Cdkey { get; set; } = new CdkeyConfigInfo();
    }
}
