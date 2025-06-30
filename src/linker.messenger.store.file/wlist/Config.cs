using linker.messenger.cdkey;
using linker.messenger.wlist;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigClientInfo
    {
        /// <summary>
        /// 白名单配置
        /// </summary>
        public WhiteListConfigInfo WhiteList { get; set; } = new WhiteListConfigInfo();
    }
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 白名单配置
        /// </summary>
        public WhiteListConfigInfo WhiteList { get; set; } = new WhiteListConfigInfo();
    }
}
