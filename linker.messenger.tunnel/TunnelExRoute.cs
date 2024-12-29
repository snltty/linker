
using linker.messenger.exroute;
using System.Net;

namespace linker.messenger.tunnel
{
    public sealed class TunnelExRoute : IExRoute
    {
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        public TunnelExRoute(TunnelNetworkTransfer tunnelNetworkTransfer)
        {
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
        }
        public List<IPAddress> Get()
        {
            return tunnelNetworkTransfer.Info.LocalIPs.Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //路由上的IP
                .Concat(tunnelNetworkTransfer.Info.RouteIPs).ToList();
        }
    }
}
