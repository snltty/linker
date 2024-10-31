using linker.client.config;
using linker.plugins.tunnel.excludeip;

namespace linker.plugins.tuntap
{
    public sealed class ExcludeIP : ITunnelExcludeIP
    {
        private readonly RunningConfig runningConfig;
        public ExcludeIP(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }
        public ExcludeIPItem[] Get()
        {
            //网卡IP不参与打洞
            return new ExcludeIPItem[] { new ExcludeIPItem { IPAddress = runningConfig.Data.Tuntap.IP, Mask = 32 } };
        }
    }
}
