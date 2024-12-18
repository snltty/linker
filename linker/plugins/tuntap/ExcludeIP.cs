using linker.messenger.tunnel;
using linker.plugins.route;
using System.Net;

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

    public sealed class RouteExcludeIPTuntap : IRouteExcludeIP
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public RouteExcludeIPTuntap(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            return new List<IPAddress> { tuntapConfigTransfer.IP };
        }
    }
}
