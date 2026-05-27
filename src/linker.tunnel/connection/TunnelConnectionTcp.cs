using linker.libs;
using linker.libs.extends;
using System.Buffers;
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
        }

        public string RemoteMachineId { get; init; }
        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public Dictionary<string, string> Configure { get; init; }
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
        public bool Connected => Socket != null && LastTicks.Expired(60000) == false;
        public int Delay { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

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

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Guid.NewGuid()}.tcp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Guid.NewGuid()}.tcp.pong");

        const byte PacketTypeData = 0;
        const byte PacketTypePing = 1;
        const byte PacketTypePong = 2;

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
                    do
                    {
                        int packetLength = memory.ToUInt16();
                        await WritePacket(memory.Slice(2, packetLength)).ConfigureAwait(false);
                        memory = memory.Slice(2 + packetLength);

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
        private ValueTask WritePacket(ReadOnlyMemory<byte> memory)
        {
            LastTicks.Update();
            try
            {
                if (memory.Span[0] == PacketTypeData)
                {
                    return callback.Receive(this, memory.Slice(2), this.userToken);
                }
                else if (memory.Span[0] == PacketTypePing)
                {
                    return SendPingPong(pongBytes, PacketTypePong);
                }
                else if (memory.Span[0] == PacketTypePong)
                {
                    Delay = (int)pingTicks.Diff();
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(string.Join(",", memory.ToArray()));
            }
            return ValueTask.CompletedTask;
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
                        pingTicks.Update();
                        await SendPingPong(pingBytes, PacketTypePing).ConfigureAwait(false);

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
        private async ValueTask SendPingPong(byte[] data, byte value)
        {
            byte[] heartData = ArrayPool<byte>.Shared.Rent(4 + data.Length);
            ((ushort)data.Length).ToBytes(heartData.AsSpan());
            data.AsMemory().CopyTo(heartData.AsMemory(4));
            heartData[2] = value;

            await SendAsync(heartData.AsMemory(0, 4 + data.Length));

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private async Task Sender()
        {
            try
            {
                byte[] bytes = new byte[16 * 1024];
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
            if (callback == null) return false;

            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                await packetEncoder.WriteAsync(data, cts.Token).ConfigureAwait(false);
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
            LastTicks.Clear();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            callback?.Closed(this, userToken);
            callback = null;
            userToken = null;
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
