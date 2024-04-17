using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.transport
{
    public interface ITransport
    {
        public Task<Socket> ConnectAsync(IPEndPoint local, IPEndPoint remote);
    }
}
