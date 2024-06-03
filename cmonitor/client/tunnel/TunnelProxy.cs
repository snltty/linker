using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.client.tunnel
{
    public partial class TunnelProxy : ITunnelConnectionReceiveCallback
    {
        private readonly NumberSpace ns = new NumberSpace();
        private SemaphoreSlim semaphoreSlimForward = new SemaphoreSlim(10);
        private SemaphoreSlim semaphoreSlimReverse = new SemaphoreSlim(10);
        public TunnelProxy()
        {
        }

        public void Start(int port)
        {
            try
            {
                StartTcp(port);
                StartUdp(port);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        protected void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(this, new AsyncUserTunnelToken
            {
                Connection = connection,
                Proxy = new ProxyInfo { }
            });
        }
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            AsyncUserTunnelToken token = userToken as AsyncUserTunnelToken;
            token.Proxy.DeBytes(memory);
            await ReadConnectionPack(token).ConfigureAwait(false);
        }
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            CloseClientSocket(userToken as AsyncUserToken);
            await Task.CompletedTask;
        }
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
                case ProxyStep.Pause:
                    PauseSocket(token);
                    break;
                case ProxyStep.Close:
                    CloseSocket(token);
                    break;
                default:
                    break;
            }
        }
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
        Receive = 4,
        Pause = 8,
        Close = 16,
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

        public ushort Port { get; set; }
        public IPEndPoint SourceEP { get; set; }
        public IPEndPoint TargetEP { get; set; }

        public byte Rsv { get; set; }

        public ReadOnlyMemory<byte> Data { get; set; }

        public byte[] ToBytes(out int length)
        {
            int sourceLength = SourceEP == null ? 0 : (SourceEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;
            int targetLength = TargetEP == null ? 0 : (TargetEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 16) + 2;

            length = 4 + 8 + 1 + 1 + 1
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

            bytes[index] = (byte)Step;
            index += 1;

            bytes[index] = (byte)Protocol;
            index += 1;

            bytes[index] = (byte)Direction;
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

            Step = (ProxyStep)span[index];
            index += 1;

            Protocol = (ProxyProtocol)span[index];
            index += 1;

            Direction = (ProxyDirection)span[index];
            index += 1;

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
