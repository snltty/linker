using linker.client.config;
using linker.plugins.tunnel.excludeip;

namespace linker.plugins.tuntap
{
    public sealed class ExcludeIP : ITunnelExcludeIP
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public ExcludeIP(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public ExcludeIPItem[] Get()
        {
            //网卡IP不参与打洞
            return new ExcludeIPItem[] { new ExcludeIPItem { IPAddress = tuntapConfigTransfer.IP, Mask = 32 } };
        }
    }
}
