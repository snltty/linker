using System.Net;
using System.Net.Sockets;

namespace linker.forward
{
    public interface IForwardHook
    {
        public bool Connect(string srcId, IPEndPoint ep, ProtocolType protocol);
        public bool Forward(AsyncUserToken token);
    }
}
