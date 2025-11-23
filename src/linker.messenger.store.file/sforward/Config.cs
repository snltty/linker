
using linker.messenger.sforward;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
    }
    public sealed partial class ConfigClientInfo
    {
    }
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public SForwardConfigServerInfo SForward { get; set; } = new SForwardConfigServerInfo();
    }
}
