using linker.tunnel.wanport;
using System.Net;

namespace linker.messenger.relay.transport
{
    public sealed class TunnelWanPortProtocolRelay : ITunnelWanPortProtocol
    {
        public string Name => "relay";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Other;

        public TunnelWanPortProtocolRelay() { }

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
