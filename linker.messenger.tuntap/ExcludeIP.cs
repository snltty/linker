using linker.messenger.exroute;
using linker.messenger.tunnel;
using System.Net;

namespace linker.messenger.tuntap
{
    public sealed class TuntapTunnelExcludeIP : ITunnelClientExcludeIP
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public TuntapTunnelExcludeIP(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            //网卡IP不参与打洞
            return new List<IPAddress> { tuntapConfigTransfer.IP };
        }
    }

    public sealed class TuntapExRoute : IExRoute
    {
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public TuntapExRoute(TuntapConfigTransfer tuntapConfigTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            return new List<IPAddress> { tuntapConfigTransfer.IP };
        }
    }
}
