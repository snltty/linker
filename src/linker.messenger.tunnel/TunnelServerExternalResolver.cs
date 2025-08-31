using System.Net.Sockets;
using System.Net;
using System.Buffers;
using linker.libs.extends;
using linker.libs;
using System.Text;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 外网端口处理器
    /// </summary>
    public class TunnelServerExternalResolver : IResolver
    {
        public byte Type => (byte)ResolverType.External;

        public virtual void Add(long recvBytes, long sendtBytes) { }

        /// <summary>
        /// UDP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ep"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{ep} get udp external port");

            Add(memory.Length, 0);

            byte[] sendData = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                Memory<byte> send = BuildSendData(sendData, ep);
                await socket.SendToAsync(send, SocketFlags.None, ep).ConfigureAwait(false);
                Add(0, send.Length);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Error(ex);
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
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{socket.RemoteEndPoint} get tcp external port");
            byte[] sendData = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                memory = BuildSendData(sendData, socket.RemoteEndPoint as IPEndPoint);
                Add(0, memory.Length);
                await socket.SendAsync(memory, SocketFlags.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Error(ex);
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

            byte[] temp = Encoding.UTF8.GetBytes(Environment.TickCount64.ToString().Sha256().SubStr(0, new Random().Next(16, 32)));
            temp.AsMemory().CopyTo(data.AsMemory(1 + length + 2));

            return data.AsMemory(0, 1 + length + 2 + temp.Length);
        }
    }

}