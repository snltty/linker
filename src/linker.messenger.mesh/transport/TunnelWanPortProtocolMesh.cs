using linker.tunnel.wanport;
using System.Net;

namespace linker.messenger.mesh
{
    public sealed class TunnelWanPortProtocolMesh : ITunnelWanPortProtocol
    {
        public string Name => "mesh";

        public TunnelWanPortProtocolType ProtocolType => TunnelWanPortProtocolType.Other;

        public TunnelWanPortProtocolMesh() { }

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
