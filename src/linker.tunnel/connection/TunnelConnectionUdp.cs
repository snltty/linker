using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace linker.tunnel.connection
{
    /// <summary>
    /// UDP隧道
    /// </summary>
    public sealed class TunnelConnectionUdp : ITunnelConnection
    {
        public TunnelConnectionUdp()
        {
        }

        public string RemoteMachineId { get; init; }
        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public string TransactionTag { get; init; }
        public string TransportName { get; init; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public string NodeId { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }
        public bool SSL { get; init; }
        public byte BufferSize { get; init; } = 3;

        public bool Connected => UdpClient != null && LastTicks.Expired(60000) == false;
        public int Delay { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();
        public bool Proxy { get; set; }

        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        private long sendRemaining = 0;
        public long SendBufferRemaining { get => sendRemaining; }
        public long SendBufferFree { get => maxRemaining - sendRemaining; }
        private const long maxRemaining = 128 * 1024;

        public long RecvBufferRemaining { get; }
        public long RecvBufferFree { get => maxRemaining; }

        public bool Receive { get; init; } = true;
        public bool Send { get; set; } = true;

        [JsonIgnore]
        public Socket UdpClient { get; init; }
        [JsonIgnore]
        public ICrypto Crypto { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");
        private readonly byte[] finBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.fing");
        private readonly byte[] encodeBuffer = new byte[8 * 1024];
        private readonly byte[] decodeBuffer = new byte[8 * 1024];

        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cts = new CancellationTokenSource();

            if (Receive)
            {
                _ = ProcessWrite();
            }
            if (Send)
            {
                _ = ProcessHeart();
            }
        }
        private async Task ProcessWrite()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            IPEndPoint ep = new IPEndPoint(UdpClient.DualMode || IPEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    SocketReceiveFromResult result = await UdpClient.ReceiveFromAsync(buffer.AsMemory(), ep, cts.Token).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"tunnel connection writer offline 0");
                        continue;
                    }
                    if (Send)
                    {
                        await CallbackPacket(buffer, 0, result.ReceivedBytes).ConfigureAwait(false);
                    }
                    else
                    {
                        await callback.Receive(this, buffer.AsMemory(0, result.ReceivedBytes), this.userToken).ConfigureAwait(false);
                    }
                    ReceiveBytes += result.ReceivedBytes;
                    LastTicks.Update();
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
                ArrayPool<byte>.Shared.Return(buffer);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {cts.IsCancellationRequested}");
                LoggerHelper.Instance.Error($"tunnel connection dispose 6");
                Dispose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");
            }
        }
        public async Task<bool> ProcessWrite(byte[] buffer, int offset, int length)
        {
            if (callback == null)
            {
                return false;
            }
            if (Send)
            {
                await CallbackPacket(buffer, offset, length).ConfigureAwait(false);
            }
            else
            {
                await callback.Receive(this, buffer.AsMemory(offset, length), this.userToken).ConfigureAwait(false);
            }
            ReceiveBytes += length;
            LastTicks.Update();

            return true;
        }
        private async Task CallbackPacket(byte[] buffer, int offset, int length)
        {
            Memory<byte> memory = buffer.AsMemory(offset, length);
            try
            {
                if (SSL)
                {
                    Crypto.TryDecode(buffer.AsSpan(offset, length), decodeBuffer, out int bytesWritten);
                    memory = decodeBuffer.AsMemory(0, bytesWritten);
                }
                if (memory.Length == pingBytes.Length && memory.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
                {
                    if (memory.Span.SequenceEqual(pingBytes))
                    {
                        await SendPingPong(pongBytes).ConfigureAwait(false);
                    }
                    else if (memory.Span.SequenceEqual(pongBytes))
                    {
                        Delay = (int)pingTicks.Diff();
                    }
                    else if (memory.Span.SequenceEqual(finBytes))
                    {
                        LoggerHelper.Instance.Error($"tunnel connection dispose 5");
                        Dispose();
                    }
                    return;
                }
                await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                    LoggerHelper.Instance.Error(Encoding.UTF8.GetString(memory.Span));
                    LoggerHelper.Instance.Error(string.Join(",", memory.ToArray()));
                }

            }
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        LoggerHelper.Instance.Error($"tunnel connection dispose 4");
                        Dispose();
                        break;
                    }

                    if (LastTicks.DiffGreater(3000))
                    {
                        pingTicks.Update();
                        await SendPingPong(pingBytes).ConfigureAwait(false);
                    }
                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        private async Task SendPingPong(byte[] data)
        {
            byte[] heartData = ArrayPool<byte>.Shared.Rent(4 + data.Length);

            data.Length.ToBytes(heartData.AsMemory());
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            await SendAsync(heartData.AsMemory(0, 4 + data.Length)).ConfigureAwait(false);

            ArrayPool<byte>.Shared.Return(heartData);
        }


        private readonly SemaphoreSlim slm = new SemaphoreSlim(1);
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                if (SSL)
                {
                    Crypto.TryEncode(data.Span.Slice(4), encodeBuffer, out int bytesWritten);
                    await UdpClient.SendToAsync(encodeBuffer.AsMemory(0, bytesWritten), IPEndPoint, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    await UdpClient.SendToAsync(data.Slice(4), IPEndPoint, cts.Token).ConfigureAwait(false);
                }
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
                slm.Release();
            }
            return false;
        }
        public async Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return await SendAsync(buffer.AsMemory(offset, length)).ConfigureAwait(false);
        }

        private async void FlushTask()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(2));

            try
            {
                while (cts.IsCancellationRequested == false && await timer.WaitForNextTickAsync(cts.Token))
                {
                    //encoder.TryFlushExpiredRepairs(buffer, out var bytesWritten, out var packetCount);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            if (callback == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            SendPingPong(finBytes).ContinueWith((result) =>
            {

                LastTicks.Clear();
                if (Receive == true)
                    UdpClient?.SafeClose();

                cts?.Cancel();
                callback?.Closed(this, userToken);
                callback = null;
                userToken = null;

                Crypto?.Dispose();

                GC.Collect();
            });
        }
        public override string ToString()
        {
            return this.ToJsonFormat();
        }
        public bool Equals(ITunnelConnection connection)
        {
            return connection != null && GetHashCode() == connection.GetHashCode() && TransactionId == connection.TransactionId && IPEndPoint.Equals(connection.IPEndPoint);
        }
    }
}