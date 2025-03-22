
using linker.messenger.exroute;
using System.Net;

namespace linker.messenger.tunnel
{
    public sealed class TunnelExRoute : IExRoute
    {
        private readonly ITunnelClientStore tunnelClientStore;
        public TunnelExRoute(ITunnelClientStore tunnelClientStor)
        {
            this.tunnelClientStore = tunnelClientStor;
        }
        public List<IPAddress> Get()
        {
            return tunnelClientStore.Network.LocalIPs.Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //路由上的IP
                .Concat(tunnelClientStore.Network.RouteIPs).ToList();
        }
    }
}
