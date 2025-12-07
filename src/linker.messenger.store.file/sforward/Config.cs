using linker.messenger.sforward.server;

namespace linker.messenger.store.file
{
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public SForwardServerConfigInfo SForward { get; set; } = new SForwardServerConfigInfo();
    }
}
