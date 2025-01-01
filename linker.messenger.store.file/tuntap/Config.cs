using linker.messenger.tuntap;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 虚拟网卡配置
        /// </summary>
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}