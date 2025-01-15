using linker.messenger.exroute;
using linker.messenger.tunnel;
using linker.tunnel.transport;
using System.Net;

namespace linker.messenger.tuntap
{
    public sealed class TuntapTunnelExcludeIP : ITunnelClientExcludeIP
    {
        private readonly TuntapDecenter tuntapDecenter;
        public TuntapTunnelExcludeIP(TuntapDecenter tuntapDecenter)
        {
            this.tuntapDecenter = tuntapDecenter;
        }
        public List<TunnelExIPInfo> Get()
        {
            //网卡IP和已添加到路由的局域网IP不参与打洞
            return tuntapDecenter.Infos.Values
                .Select(c => new TunnelExIPInfo { IP = c.IP, PrefixLength = c.PrefixLength })
                .Concat(tuntapDecenter.Routes.Select(c => new TunnelExIPInfo { IP = c.Address, PrefixLength = c.PrefixLength })).ToList();
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
            return new List<IPAddress> { tuntapConfigTransfer.IP }
            .Concat(tuntapConfigTransfer.Info.Lans.Where(c => c.Disabled == false).Select(c => c.IP)).ToList();
        }
    }
}
