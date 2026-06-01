using linker.libs.extends;
using linker.messenger.reverse.server;

namespace linker.messenger.store.file
{
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public ReverseServerConfigInfo Reverse { get; set; } = new ReverseServerConfigInfo();

        [SaveJsonIgnore]
        public ReverseServerConfigInfo SForward { get; set; } = new ReverseServerConfigInfo();
    }
}
