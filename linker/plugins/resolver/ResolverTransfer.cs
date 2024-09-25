using linker.libs;
using System.Net.Sockets;
using linker.libs.extends;
using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Net;

namespace linker.plugins.resolver
{
    public sealed class ResolverTransfer
    {
        private readonly Dictionary<ResolverType, IResolver> resolvers = new Dictionary<ResolverType, IResolver>();

        private readonly ServiceProvider serviceProvider;
        public ResolverTransfer(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public void LoadResolvers(Assembly[] assemblys)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IResolver)).Distinct();
            foreach (Type type in types)
            {
                IResolver resolver = (IResolver)serviceProvider.GetService(type);
                if (resolver == null)
                {
                    continue;
                }
                LoggerHelper.Instance.Info($"load Resolver:{type.Name}");

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
                socket.KeepAlive();

                await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None).ConfigureAwait(false);
                ResolverType type = (ResolverType)buffer[0];

                if (resolvers.TryGetValue(type, out IResolver resolver))
                {
                    await resolver.Resolve(socket);
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
                await resolver.Resolve(socket, ep, memory);
            }
        }
    }
}
