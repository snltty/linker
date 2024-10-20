using System.Net.Sockets;
using System.Net;
using System.Buffers;
using linker.libs.extends;
using linker.messenger.resolver;
using System;
using System.Threading.Tasks;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public class ExternalResolver : IResolver
    {
        public ResolverType Type => ResolverType.External;

        public ExternalResolver()
        {
        }

        public virtual void OnSendt( ulong length)
        {

        }
        public virtual void OnReceived( ulong length)
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
            OnReceived((ulong)memory.Length);
            byte[] sendData = ArrayPool<byte>.Shared.Rent(20);
            try
            {
                var send = BuildSendData(sendData, ep);
                OnSendt((ulong)send.Length);
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
                OnSendt((ulong)memory.Length);
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