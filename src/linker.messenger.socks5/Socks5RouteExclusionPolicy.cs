using linker.messenger.rpolicy;
using System.Net;

namespace linker.messenger.socks5
{
    public sealed class Socks5RouteExclusionPolicy : IRouteExclusionPolicy
    {
        private readonly ISocks5Store socks5Store;
        public Socks5RouteExclusionPolicy(ISocks5Store socks5Store)
        {
            this.socks5Store = socks5Store;
        }
        public List<IPAddress> Query()
        {
            return socks5Store.Lans.Select(c => c.IP).ToList();
        }
    }
}
