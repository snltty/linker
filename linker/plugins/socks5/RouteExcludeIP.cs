using linker.plugins.route;
using System.Net;

namespace linker.plugins.socks5
{
    public sealed class RouteExcludeIPSocks5 : IRouteExcludeIP
    {
        private readonly Socks5ConfigTransfer socks5ConfigTransfer;
        public RouteExcludeIPSocks5(Socks5ConfigTransfer socks5ConfigTransfer)
        {
            this.socks5ConfigTransfer = socks5ConfigTransfer;
        }
        public List<IPAddress> Get()
        {
            return socks5ConfigTransfer.Lans.Select(c => c.IP).ToList();
        }
    }
}
