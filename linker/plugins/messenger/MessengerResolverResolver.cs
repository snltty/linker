using linker.plugins.resolver;
using System.Net.Sockets;
using System.Net;
using linker.messenger;

namespace linker.plugins.messenger
{
    public sealed class MessengerResolverResolver : IResolver
    {
        public ResolverType Type => ResolverType.Messenger;

        private readonly IMessengerResolver messengerResolver;
        public MessengerResolverResolver(IMessengerResolver messengerResolver)
        {
            this.messengerResolver = messengerResolver;
        }
        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await messengerResolver.BeginReceiveServer(socket, memory);
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await messengerResolver.BeginReceiveServer(socket, ep, memory);
        }
    }
}
