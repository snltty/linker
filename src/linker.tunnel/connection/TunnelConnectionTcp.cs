using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace linker.tunnel.connection
{
    /// <summary>
    /// TCP隧道
    /// </summary>
    public sealed class TunnelConnectionTcp : ITunnelConnection
    {
        public TunnelConnectionTcp()
        {
            HashCode = GetHashCode();
        }

        public string RemoteMachineId { get; set; }
        public string RemoteMachineName { get; set; }
        public string TransactionId { get; set; }
        public Dictionary<string, string> Configure { get; init; }
        public string TransportName { get; set; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; set; }
        public TunnelType Type { get; set; }
        public string NodeId { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }
        public bool SSL { get; init; }
        public byte BufferSize { get; init; } = 3;
        public bool Connected => Socket != null && LastTicks.Expired(60000) == false;
        public int Delay { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        public int HashCode { get; private set; }

        private const long maxRemaining = 128 * 1024;
        private readonly StickyPacketCodec packetEncoder = new StickyPacketCodec(maxRemaining);
        public long SendBytes => packetEncoder.SendBytes;
        public long SendBufferRemaining => packetEncoder.SendBufferRemaining;
        public long SendBufferFree => packetEncoder.SendBufferFree;
        private readonly StickyPacketCodec packetDecoder = new StickyPacketCodec(maxRemaining);
        public long ReceiveBytes => packetDecoder.SendBytes;
        public long RecvBufferRemaining => packetDecoder.SendBufferRemaining;
        public long RecvBufferFree => packetDecoder.SendBufferFree;

        [JsonIgnore]
        public SslStream Stream { get; init; }
        [JsonIgnore]
        public Socket Socket { get; init; }

        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;

        private readonly LastTicksManager pingPongTicks = new LastTicksManager();

        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cts = new CancellationTokenSource();

            _ = Sender();
            _ = Recver();

            _ = ProcessWrite();
            _ = ProcessHeart();
        }

        private async Task ProcessWrite()
        {
            try
            {
                if (Stream != null)
                {
                    while (cts.IsCancellationRequested == false)
                    {
                        Memory<byte> memory = packetDecoder.GetMemory(8 * 1024);
                        int length = await Stream.ReadAsync(memory, cts.Token).ConfigureAwait(false);
                        if (length == 0) break;
                        await packetDecoder.FlushAsync(length, cts.Token).ConfigureAwait(false);
                    }
                }
                else
                {
                    while (cts.IsCancellationRequested == false)
                    {
                        Memory<byte> memory = packetDecoder.GetMemory(8 * 1024);
                        int length = await Socket.ReceiveAsync(memory, SocketFlags.None, cts.Token).ConfigureAwait(false);
                        if (length == 0) break;
                        await packetDecoder.FlushAsync(length, cts.Token).ConfigureAwait(false);
                    }
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
            }
        }
        private async Task Recver()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    ReadOnlyMemory<byte> memory = await packetDecoder.ReadPacketsAsync(cts.Token).ConfigureAwait(false);
                    if (memory.IsEmpty)
                    {
                        if (packetDecoder.IsCompleted)
                        {
                            cts.Cancel();
                            break;
                        }
                        continue;
                    }
                    int length = memory.Length;
                    do
                    {
                        try
                        {
                            int packetLength = packetDecoder.ReadLength(memory);
                            await ProcessPacket(memory.Slice(StickyPacketCodec.PacketLengthSize, packetLength)).ConfigureAwait(false);
                            memory = memory.Slice(StickyPacketCodec.PacketLengthSize + packetLength);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Instance.Error($"TCP process packet error:{ex}");
                            break;
                        }

                    } while (memory.Length > 0);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Dispose();
            }
        }
        private ValueTask<bool> ProcessPacket(ReadOnlyMemory<byte> memory)
        {
            LastTicks.Update();
            TunnelPacket packet = new TunnelPacket(memory, false);
            return packet.Flag switch
            {
                TunnelPacket.PacketFlagData => callback.Receive(this, packet.PayloadData, this.userToken),
                TunnelPacket.PacketFlagPing => SendPingPong(Encoding.UTF8.GetBytes($"{Guid.NewGuid()}"), TunnelPacket.PacketFlagPong),
                TunnelPacket.PacketFlagPong => ProcessPong(),
                _ => ValueTask.FromResult(true)
            };
        }
        private ValueTask<bool> ProcessPong()
        {
            Delay = (int)pingPongTicks.Diff();
            return ValueTask.FromResult(true);
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        Dispose();
                        break;
                    }

                    if (LastTicks.DiffGreater(3000))
                    {
                        pingPongTicks.Update();
                        await SendPingPong(Encoding.UTF8.GetBytes($"{Guid.NewGuid()}"), TunnelPacket.PacketFlagPing).ConfigureAwait(false);

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
        private async ValueTask<bool> SendPingPong(byte[] data, byte value)
        {
            byte[] heartData = ArrayPool<byte>.Shared.Rent(TunnelPacket.PacketHeaderSize + data.Length);

            TunnelPacket packet = new TunnelPacket(heartData, data, value, 0);
            await SendAsync(packet.RawData).ConfigureAwait(false);

            ArrayPool<byte>.Shared.Return(heartData);

            return true;
        }

        private async Task Sender()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {

                    ReadResult result = await packetEncoder.ReadAsync(cts.Token).ConfigureAwait(false);
                    if (result.Buffer.IsEmpty)
                    {
                        if (packetEncoder.IsCompleted)
                        {
                            cts.Cancel();
                            break;
                        }
                        continue;
                    }
                    if (Stream != null)
                    {
                        foreach (ReadOnlyMemory<byte> segment in result.Buffer)
                        {
                            await Stream.WriteAsync(segment, cts.Token).ConfigureAwait(false);
                            packetEncoder.Advance(segment.Length);
                        }
                    }
                    else
                    {
                        foreach (ReadOnlyMemory<byte> segment in result.Buffer)
                        {
                            await Socket.SendAllAsync(segment, cts.Token).ConfigureAwait(false);
                            packetEncoder.Advance(segment.Length);
                        }
                    }

                    packetEncoder.AdvanceTo(result.Buffer.End);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Dispose();
            }

        }
        private readonly SemaphoreSlim slm = new SemaphoreSlim(1);
        public async ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (cts.IsCancellationRequested || data.Length > 10 * 1024)
            {
                return false;
            }

            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                TunnelPacket packet = new TunnelPacket(data);
                if (packet.Length != packet.Payload.Length)
                {
                    LoggerHelper.Instance.Error($"tcp tunnel data length mismatch {packet.Length} != {packet.Payload.Length}");
                    return false;
                }
                await packetEncoder.WriteAsync(packet.RawData, cts.Token).ConfigureAwait(false);
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
        public ValueTask<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return SendAsync(buffer.AsMemory(offset, length));
        }

        public void Dispose()
        {
            if (callback == null) return;

            var _callback = callback;
            callback = null;

            LastTicks.Clear();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            cts?.Cancel();

            Stream?.Close();
            Stream?.Dispose();

            Socket?.SafeClose();

            try
            {
                packetEncoder?.Dispose();
                packetDecoder?.Dispose();
            }
            catch (Exception)
            { }
            GC.Collect();

            _callback?.Closed(this, userToken);
            userToken = null;

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
