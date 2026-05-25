using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Quic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace linker.tunnel.connection
{
    /// <summary>
    /// QUIC隧道
    /// </summary>
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
    public sealed class TunnelConnectionQuic : ITunnelConnection
    {
        public TunnelConnectionQuic()
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
        public bool SSL => true;
        public byte BufferSize { get; init; } = 3;

        public bool Connected => Stream != null && Stream.CanWrite && LastTicks.HasValue();
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
        public QuicStream Stream { get; init; }
        [JsonIgnore]
        public QuicConnection Connection { get; init; }

        [JsonIgnore]
        public Socket QuicUdp { get; init; }
        [JsonIgnore]
        public Socket RemoteUdp { get; init; }

        public ITunnelConnection OriginConnection { get; init; }

        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.quic.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.quic.pong");

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
                int length = 0;
                while (cts.IsCancellationRequested == false)
                {
                    Memory<byte> memory = packetDecoder.GetMemory(8 * 1024);
                    length = await Stream.ReadAsync(memory, cts.Token).ConfigureAwait(false);
                    if (length == 0) break;

                    await packetDecoder.FlushAsync(length, cts.Token).ConfigureAwait(false);
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
                        int packetLength = memory.ToInt32();
                        await WritePacket(memory.Slice(4, packetLength)).ConfigureAwait(false);
                        memory = memory.Slice(4 + packetLength);

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
        private async Task WritePacket(ReadOnlyMemory<byte> packet)
        {
            LastTicks.Update();
            if (packet.Length == pingBytes.Length && packet.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
            {
                if (packet.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes).ConfigureAwait(false);
                    return;
                }
                else if (packet.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)pingTicks.Diff();
                    return;
                }
            }
            try
            {
                await callback.Receive(this, packet, this.userToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(string.Join(",", packet.ToArray()));
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
            int length = 4 + data.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData.AsSpan());
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            await SendAsync(heartData.AsMemory(0, length)).ConfigureAwait(false);

            ArrayPool<byte>.Shared.Return(heartData);
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

                    foreach (ReadOnlyMemory<byte> memoryBlock in result.Buffer)
                    {
                        await Stream.WriteAsync(memoryBlock, cts.Token).ConfigureAwait(false);
                        packetEncoder.Advance(memoryBlock.Length);
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
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (callback == null) return false;

            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                await packetEncoder.WriteAsync(data, cts.Token).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
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
            Connection?.CloseAsync(0x0a);
            Connection?.DisposeAsync();

            QuicUdp?.Close();


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
