using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.tunnel.proxy
{
    public partial class TunnelProxy : ITunnelConnectionReceiveCallback
    {
        private readonly NumberSpace ns = new NumberSpace();
        private SemaphoreSlim semaphoreSlimForward = new SemaphoreSlim(10);
        private SemaphoreSlim semaphoreSlimReverse = new SemaphoreSlim(10);

        public virtual IPAddress UdpBindAdress { get; set; }

        public TunnelProxy()
        {
            TaskUdp();
        }

        public void Start(IPEndPoint ep, byte bufferSize)
        {
            StartTcp(ep, bufferSize);
            StartUdp(new IPEndPoint(ep.Address, LocalEndpoint.Port), bufferSize);
        }

        /// <summary>
        /// 接收隧道的数据
        /// </summary>
        /// <param name="connection"></param>
        protected void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(this, new AsyncUserTunnelToken
            {
                Connection = connection,
                Proxy = new ProxyInfo { }
            });
        }
        /// <summary>
        /// 收到隧道数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="memory"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            AsyncUserTunnelToken token = userToken as AsyncUserTunnelToken;

            token.Proxy.DeBytes(memory);
            await ReadConnectionPack(token).ConfigureAwait(false);
        }
        /// <summary>
        /// 收到隧道关闭消息
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            try
            {
                CloseClientSocketTcp(connection);
                CloseClientSocketUdp(connection);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            await Task.CompletedTask;
        }
        /// <summary>
        /// 根据不同的消息类型做不同的事情
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ReadConnectionPack(AsyncUserTunnelToken token)
        {
            switch (token.Proxy.Step)
            {
                case ProxyStep.Request:
                    ConnectBind(token);
                    break;
                case ProxyStep.Forward:
                    await SendToSocket(token).ConfigureAwait(false);
                    break;
                case ProxyStep.Receive:
                    ReceiveSocket(token);
                    break;
                case ProxyStep.Close:
                    CloseSocket(token);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 发送数据到目标服务
        /// </summary>
        /// <param name="tunnelToken"></param>
        /// <returns></returns>
        private async Task SendToSocket(AsyncUserTunnelToken tunnelToken)
        {
            if (tunnelToken.Proxy.Protocol == ProxyProtocol.Tcp)
            {
                await SendToSocketTcp(tunnelToken).ConfigureAwait(false);
            }
            else
            {
                await SendToSocketUdp(tunnelToken).ConfigureAwait(false);
            }
        }


        public virtual void Stop()
        {
            StopTcp();
            StopUdp();
        }
        public virtual void Stop(int port)
        {
            StopTcp(port);
            StopUdp(port);
        }

    }

    public enum ProxyStep : byte
    {
        Request = 1,
        Forward = 2,
        Receive = 3,
        Pause = 4,
        Close = 5,
    }
    public enum ProxyProtocol : byte
    {
        Tcp = 0,
        Udp = 1
    }
    public enum ProxyDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public sealed class ProxyInfo
    {
        public ulong ConnectId { get; set; }
        public ProxyStep Step { get; set; } = ProxyStep.Request;
        public ProxyProtocol Protocol { get; set; } = ProxyProtocol.Tcp;
        public ProxyDirection Direction { get; set; } = ProxyDirection.Forward;
        public byte BufferSize { get; set; } = 3;

        public ushort Port { get; set; }
        public IPEndPoint SourceEP { get; set; }
        public IPEndPoint TargetEP { get; set; }

        public byte Rsv { get; set; }


        public ReadOnlyMemory<byte> Data { get; set; }

        public byte[] ToBytes(out int length)
        {
            int sourceLength = SourceEP == null ? 0 : (SourceEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;
            int targetLength = TargetEP == null ? 0 : (TargetEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;

            length = 4 + 8 + 1 + 1
                + 2
                + 1 + sourceLength
                + 1 + targetLength
                + Data.Length;

            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
            Memory<byte> memory = bytes.AsMemory();

            int index = 0;

            (length - 4).ToBytes(memory);
            index += 4;


            ConnectId.ToBytes(memory.Slice(index));
            index += 8;

            bytes[index] = (byte)((byte)Step << 4 | (byte)Protocol << 2 | (byte)Direction);
            index += 1;

            bytes[index] = BufferSize;
            index += 1;

            Port.ToBytes(memory.Slice(index));
            index += 2;

            bytes[index] = (byte)sourceLength;
            index += 1;

            if (sourceLength > 0)
            {
                SourceEP.Address.TryWriteBytes(memory.Slice(index).Span, out int writeLength);
                index += writeLength;

                ((ushort)SourceEP.Port).ToBytes(memory.Slice(index));
                index += 2;
            }


            bytes[index] = (byte)targetLength;
            index += 1;

            if (targetLength > 0)
            {
                TargetEP.Address.TryWriteBytes(memory.Slice(index).Span, out int writeLength);
                index += writeLength;

                ((ushort)TargetEP.Port).ToBytes(memory.Slice(index));
                index += 2;
            }

            Data.CopyTo(memory.Slice(index));

            return bytes;

        }

        public void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            int index = 0;
            ReadOnlySpan<byte> span = memory.Span;

            ConnectId = memory.Slice(index).ToUInt64();
            index += 8;

            Step = (ProxyStep)(span[index] >> 4);
            Protocol = (ProxyProtocol)(span[index] & 0b1100);
            Direction = (ProxyDirection)(span[index] & 0b0011);
            index++;

            BufferSize = span[index];
            index++;

            Port = memory.Slice(index).ToUInt16();
            index += 2;

            byte sourceLength = span[index];
            index += 1;
            if (sourceLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, sourceLength - 2));
                index += sourceLength;
                ushort port = span.Slice(index - 2).ToUInt16();
                SourceEP = new IPEndPoint(ip, port);
            }

            byte targetLength = span[index];
            index += 1;
            if (targetLength > 0)
            {
                IPAddress ip = new IPAddress(span.Slice(index, targetLength - 2));
                index += targetLength;
                ushort port = span.Slice(index - 2).ToUInt16();
                TargetEP = new IPEndPoint(ip, port);
            }
            Data = memory.Slice(index);
        }
    }

    public sealed class AsyncUserTunnelToken
    {
        public ITunnelConnection Connection { get; set; }

        public ProxyInfo Proxy { get; set; }

        public void Clear()
        {
            GC.Collect();
        }
    }

}
