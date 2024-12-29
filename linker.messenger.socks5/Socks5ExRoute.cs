using linker.messenger.exroute;
using System.Net;

namespace linker.messenger.socks5
{
    public sealed class Socks5ExRoute : IExRoute
    {
        private readonly ISocks5Store socks5Store;
        public Socks5ExRoute(ISocks5Store socks5Store)
        {
            this.socks5Store = socks5Store;
        }
        public List<IPAddress> Get()
        {
            return socks5Store.Lans.Select(c => c.IP).ToList();
        }
    }
}
