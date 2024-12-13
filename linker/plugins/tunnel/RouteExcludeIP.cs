using linker.plugins.route;
using System.Net;

namespace linker.plugins.tunnel
{
    public sealed class RouteExcludeIPTunnel : IRouteExcludeIP
    {
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        public RouteExcludeIPTunnel(TunnelConfigTransfer tunnelConfigTransfer)
        {
            this.tunnelConfigTransfer = tunnelConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            return tunnelConfigTransfer.LocalIPs.Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //路由上的IP
                .Concat(tunnelConfigTransfer.RouteIPs).ToList();
        }
    }
}
