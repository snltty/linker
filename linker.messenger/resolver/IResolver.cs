using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace linker.messenger.resolver
{
    public enum ResolverType : byte
    {
        External = 0,
        Messenger = 1,
        Relay = 2,
        Socks4 = 4,
        Socks5 = 5,
        Flow = 6
    }
    public interface IResolver
    {
        public ResolverType Type { get; }
        public Task Resolve(Socket socket, Memory<byte> memory);
        public Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory);
    }


}
