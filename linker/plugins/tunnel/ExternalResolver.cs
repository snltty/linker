using System.Net.Sockets;
using System.Net;
using System.Buffers;
using linker.libs.extends;
using linker.plugins.resolver;
using linker.plugins.flow;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public sealed class ExternalResolver : IResolver, IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "External";

        public ResolverType Type => ResolverType.External;

        public ExternalResolver()
        {
        }

        /// <summary>
        /// UDP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ep"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            ReceiveBytes += (ulong)memory.Length;
            byte[] sendData = ArrayPool<byte>.Shared.Rent(20);
            try
            {
                var send = BuildSendData(sendData, ep);
                SendtBytes += (ulong)send.Length;
                await socket.SendToAsync(send, SocketFlags.None, ep).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendData);
            }
        }
        /// <summary>
        /// TCP
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            byte[] sendData = ArrayPool<byte>.Shared.Rent(20);
            try
            {
                memory = BuildSendData(sendData, socket.RemoteEndPoint as IPEndPoint);
                SendtBytes += (ulong)memory.Length;
                await socket.SendAsync(memory, SocketFlags.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendData);
            }
        }
        private Memory<byte> BuildSendData(byte[] data, IPEndPoint ep)
        {
            //给客户端返回他的IP+端口
            data[0] = (byte)ep.AddressFamily;
            ep.Address.TryWriteBytes(data.AsSpan(1), out int length);
            ((ushort)ep.Port).ToBytes(data.AsMemory(1 + length));

            //防止一些网关修改掉它的外网IP
            for (int i = 0; i < 1 + length + 2; i++)
            {
                data[i] = (byte)(data[i] ^ byte.MaxValue);
            }
            return data.AsMemory(0, 1 + length + 2);
        }
    }

}