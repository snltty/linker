using linker.messenger.rpolicy;
using System.Net;

namespace linker.messenger.tunnel.client
{
    public sealed class TunnelRouteExclusionPolicy : IRouteExclusionPolicy
    {
        private readonly ITunnelClientStore tunnelClientStore;
        public TunnelRouteExclusionPolicy(ITunnelClientStore tunnelClientStor)
        {
            this.tunnelClientStore = tunnelClientStor;
        }
        public List<IPAddress> Query()
        {
            return tunnelClientStore.Network.LocalIPs
                .Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Concat(tunnelClientStore.Network.RouteIPs).ToList();
        }
    }
}
