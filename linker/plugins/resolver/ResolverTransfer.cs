using linker.libs;
using System.Net.Sockets;
using linker.libs.extends;
using System.Buffers;
using System.Net;
using linker.plugins.messenger;

namespace linker.plugins.resolver
{
    public sealed class ResolverTransfer
    {
        private readonly Dictionary<ResolverType, IResolver> resolvers = new Dictionary<ResolverType, IResolver>();

        public ResolverTransfer()
        {
        }
        public void LoadResolvers(List<IResolver> list)
        {
            foreach (IResolver resolver in list)
            {
                resolvers.TryAdd(resolver.Type, resolver);
            }
        }


        public async Task BeginReceive(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }
                LoggerHelper.Instance.Info($"tcp connect from {socket.RemoteEndPoint}");
                socket.KeepAlive();

                int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None).ConfigureAwait(false);
                ResolverType type = (ResolverType)buffer[0];

                if (resolvers.TryGetValue(type, out IResolver resolver))
                {
                    await resolver.Resolve(socket, buffer.AsMemory(1, length));
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public async Task BeginReceive(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            if (resolvers.TryGetValue((ResolverType)memory.Span[0], out IResolver resolver))
            {
                await resolver.Resolve(socket, ep, memory.Slice(1));
            }
        }
    }
}
