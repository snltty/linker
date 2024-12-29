using linker.messenger.tunnel;
using linker.plugins.route;
using System.Net;

namespace linker.plugins.tuntap
{
    public sealed class ExcludeIP : ITunnelClientExcludeIP
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public ExcludeIP(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            //网卡IP不参与打洞
            return new List<IPAddress> { tuntapConfigTransfer.IP };
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
