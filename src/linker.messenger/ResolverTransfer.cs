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
        public async Task BeginReceive(byte type, Socket socket)
        {
            try
            {
                if (resolvers.TryGetValue(type, out IResolver resolver))
                {
                    await resolver.Resolve(socket, Helper.EmptyArray).ConfigureAwait(false);
                }
                else
                {
                    socket.SafeClose();
                }
            }
            catch (Exception)
            {
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
