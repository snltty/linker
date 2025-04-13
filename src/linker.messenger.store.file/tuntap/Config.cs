using linker.messenger.signin;
using linker.messenger.tuntap;
using linker.messenger.tuntap.lease;
using System.Collections.Concurrent;

namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 虚拟网卡配置
        /// </summary>
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
        public ConcurrentDictionary<string, LeaseInfo> Leases { get; set; } = new ConcurrentDictionary<string, LeaseInfo>();
    }

    public partial class ConfigServerInfo
    {
        public TuntapConfigServerInfo Tuntap { get; set; } = new TuntapConfigServerInfo();
    }
}