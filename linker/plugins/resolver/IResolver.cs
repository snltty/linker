using System.Net;
using System.Net.Sockets;

namespace linker.plugins.resolver
{
    public enum ResolverType : byte
    {
        External = 0,
        Messenger = 1,
        Relay = 2,
    }
    public interface IResolver
    {
        public ResolverType Type { get; }
        public Task Resolve(Socket socket);
        public Task Resolve(Socket socket,IPEndPoint ep,Memory<byte> memory);
    }

}
