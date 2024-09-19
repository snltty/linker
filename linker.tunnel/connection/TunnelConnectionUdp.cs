using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace linker.tunnel.connection
{
    public sealed class TunnelConnectionUdp : ITunnelConnection
    {
        public TunnelConnectionUdp()
        {
        }

        public string RemoteMachineId { get; init; }
        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public string TransportName { get; init; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }
        public bool SSL { get; init; }
        public byte BufferSize { get; init; } = 3;

        public bool Connected => UdpClient != null && LastTicks > 0 && Environment.TickCount64 - LastTicks < 15000;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }
        public long LastTicks { get; private set; } = Environment.TickCount64;

        public bool Receive { get; init; }

        public Socket uUdpClient;
        [JsonIgnore]
        public Socket UdpClient
        {
            get
            {
                return uUdpClient;
            }
            init
            {
                uUdpClient = value;
            }
        }
        [JsonIgnore]
        public ICrypto Crypto { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;

        private long pingStart = Environment.TickCount64;
        private byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");
        private byte[] finBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.fing");
        private bool pong = true;


        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">数据回调</param>
        /// <param name="userToken">自定义数据</param>
        /// <param name="framing">是否处理粘包，true时，请在首部4字节标注数据长度</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool framing = true)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cancellationTokenSource = new CancellationTokenSource();

            if (Receive)
            {
                _ = ProcessWrite();
            }
            _ = ProcessHeart();

        }
        private async Task ProcessWrite()
        {
            byte[] buffer = new byte[65 * 1024];
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    SocketReceiveFromResult result = await UdpClient.ReceiveFromAsync(buffer.AsMemory(), ep, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (result.ReceivedBytes == 0)
                    {
                        break;
                    }
                    await CallbackPacket(buffer.AsMemory(0, result.ReceivedBytes)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                Dispose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");
            }
        }

        public async Task<bool> ProcessWrite(Memory<byte> packet)
        {
            if (callback == null)
            {
                return false;
            }
            await CallbackPacket(packet).ConfigureAwait(false);
            return true;
        }
        private async Task CallbackPacket(Memory<byte> packet)
        {
            ReceiveBytes += packet.Length;
            LastTicks = Environment.TickCount64;

            Memory<byte> memory = packet.Slice(4);
            if (memory.Length == pingBytes.Length && memory.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
            {
                if (memory.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes).ConfigureAwait(false);
                }
                else if (memory.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)(Environment.TickCount64 - pingStart);
                    pong = true;
                }
                else if (memory.Span.SequenceEqual(finBytes))
                {
                    Dispose();
                }
            }
            else
            {
                try
                {
                    if (SSL)
                    {
                        packet.CopyTo(decodeBuffer);
                        packet = Crypto.Decode(decodeBuffer, 0, packet.Length);
                    }

                    await callback.Receive(this, packet.Slice(4), this.userToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        Dispose();
                        break;
                    }

                    if (Environment.TickCount64 - LastTicks > 3000)
                    {
                        pingStart = Environment.TickCount64;
                        await SendPingPong(pingBytes).ConfigureAwait(false);
                    }
                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + data.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData);
            data.AsMemory().CopyTo(heartData.AsMemory(4));
            SendBytes += data.Length;
            try
            {
                await UdpClient.SendToAsync(heartData.AsMemory(0, length), IPEndPoint, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                pong = true;
                Dispose();
            }
            finally
            {
            }

            ArrayPool<byte>.Shared.Return(heartData);
        }

        public async Task SendPing()
        {
            if (pong == false) return;
            pong = false;
            pingStart = Environment.TickCount64;
            await SendPingPong(pingBytes).ConfigureAwait(false);
        }


        private byte[] encodeBuffer = new byte[2 * 1024];
        private byte[] decodeBuffer = new byte[2 * 2014];
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            try
            {
                if (SSL)
                {
                    data.CopyTo(encodeBuffer);
                    data = Crypto.Encode(encodeBuffer, 0, data.Length);
                }
                await UdpClient.SendToAsync(data, IPEndPoint, cancellationTokenSource.Token).ConfigureAwait(false);
                SendBytes += data.Length;
                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Dispose();
            }
            finally
            {
            }
            return false;
        }

        public void Dispose()
        {
            if (uUdpClient == null) return;

            SendPingPong(finBytes).ContinueWith((result) =>
            {
                LastTicks = 0;
                if (Receive == true)
                    UdpClient?.SafeClose();
                uUdpClient = null;

                cancellationTokenSource?.Cancel();
                callback?.Closed(this, userToken);
                callback = null;
                userToken = null;

                Crypto?.Dispose();
            });
        }

        public override string ToString()
        {
            return this.ToJsonFormat();
        }
        public bool Equals(ITunnelConnection connection)
        {
            return connection != null && GetHashCode() == connection.GetHashCode() && IPEndPoint.Equals(connection.IPEndPoint);
        }
    }
}
