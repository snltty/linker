using linker.plugins.resolver;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.socks5
{
    public sealed class Socks5Resolver : IResolver
    {
        public ResolverType Type => ResolverType.Socks5;

        public Socks5Resolver()
        {
        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {

            await Task.CompletedTask;
        }

    }

}
