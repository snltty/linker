using linker.tunnel.wanport;
using System.Net;

namespace linker.messenger.pcp
{
    public sealed class TunnelWanPortProtocolPcp : ITunnelWanPortProtocol
    {
        public string Name => "pcp";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Other;

        public TunnelWanPortProtocolPcp() { }

        public async Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server)
        {
            return new TunnelWanPortEndPoint
            {
                Local = new IPEndPoint(IPAddress.Loopback, 0),
                Remote = server
            };
        }
    }
}
