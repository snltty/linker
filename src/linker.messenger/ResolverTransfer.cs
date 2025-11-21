using linker.libs;
using System.Net.Sockets;
using linker.libs.extends;
using System.Buffers;
using System.Net;
namespace linker.messenger
{
    /// <summary>
    /// 消息分发器
    /// </summary>
    public sealed class ResolverTransfer
    {
        private readonly Dictionary<byte, IResolver> resolvers = new Dictionary<byte, IResolver>();

        public ResolverTransfer()
        {
        }

        /// <summary>
        /// 添加消息分发器
        /// </summary>
        /// <param name="list"></param>
        public void AddResolvers(List<IResolver> list)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add resolver {string.Join(",", list.Select(c => c.GetType().Name))}");
            foreach (IResolver resolver in list)
            {
                resolvers.TryAdd((byte)resolver.Type, resolver);
            }
        }

        /// <summary>
        /// 开始处理这个连接的分发 TCP
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public async Task BeginReceive(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(32);
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }

                int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                byte type = buffer[0];

                if (resolvers.TryGetValue(type, out IResolver resolver))
                {
                    await resolver.Resolve(socket, buffer.AsMemory(1, length)).ConfigureAwait(false);
                }
                else
                {
                    socket.SafeClose();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                socket.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        /// <summary>
        /// 开始处理这个连接的分发 UDP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ep"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async Task BeginReceive(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            if (resolvers.TryGetValue(memory.Span[0], out IResolver resolver))
            {
                await resolver.Resolve(socket, ep, memory.Slice(1)).ConfigureAwait(false);
            }
        }
    }
}
