using common.libs.extends;
using common.libs;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace cmonitor.tunnel.connection
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
        public bool SSL => false;

        public bool Connected => UdpClient != null && Environment.TickCount64 - ticks < 15000;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        [JsonIgnore]
        public UdpClient UdpClient { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;

        private long ticks = Environment.TickCount64;
        private long pingStart = Environment.TickCount64;
        private static byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private static byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");
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
            _ = ProcessWrite();
            _ = ProcessHeart();

        }
        private async Task ProcessWrite()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    UdpReceiveResult result = await UdpClient.ReceiveAsync(cancellationTokenSource.Token);
                    ReceiveBytes += result.Buffer.Length;
                    ticks = Environment.TickCount64;
                    if (result.Buffer.Length == 0)
                    {
                        break;
                    }
                    await CallbackPacket(result.Buffer).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                Dispose();
                Logger.Instance.Error($"tunnel connection writer offline {ToString()}");
            }
        }
        private async Task CallbackPacket(Memory<byte> packet)
        {
            if (packet.Length == pingBytes.Length && (packet.Span.SequenceEqual(pingBytes) || packet.Span.SequenceEqual(pongBytes)))
            {
                if (packet.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes);
                }
                else if (packet.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)(Environment.TickCount64 - pingStart);
                    pong = true;
                }
            }
            else
            {
                try
                {
                    await callback.Receive(this, packet, this.userToken).ConfigureAwait(false);
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
                    if (Environment.TickCount64 - ticks > 3000)
                    {
                        pingStart = Environment.TickCount64;
                        await SendPingPong(pingBytes);
                    }
                    await Task.Delay(3000);
                }
            }
            catch (Exception)
            {
            }
        }
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + pingBytes.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData);
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            try
            {
                await UdpClient.SendAsync(heartData.AsMemory(0, length), IPEndPoint, cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                pong = true;
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
            await SendPingPong(pingBytes);
        }
        public async ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            try
            {
                await UdpClient.SendAsync(data, IPEndPoint, cancellationTokenSource.Token);
                SendBytes += data.Length;
                ticks = Environment.TickCount64;
                return true;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
            }
            return false;
        }

        public void Dispose()
        {
            callback?.Closed(this, userToken);
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();

            UdpClient?.Close();
            UdpClient?.Dispose();
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineId:{RemoteMachineId},RemoteMachineName:{RemoteMachineName}";
        }
    }
}
