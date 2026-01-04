using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net.Quic;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace linker.tunnel.connection
{
    /// <summary>
    /// msquic隧道
    /// </summary>
    public sealed class TunnelConnectionMsQuic : ITunnelConnection
    {
        public TunnelConnectionMsQuic()
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
        public bool SSL => true;

        public byte BufferSize { get; init; } = 3;

#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        public bool Connected => Stream != null && Stream.CanWrite && LastTicks.HasValue();
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }
        public long SendBufferRemaining { get; }
        public long SendBufferFree { get; } = 512 * 1024;
        public long RecvBufferRemaining { get; }
        public long RecvBufferFree { get; } = 512 * 1024;

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        [JsonIgnore]
        public byte[] PacketBuffer { get; set; } = Helper.EmptyArray;

#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        [JsonIgnore]
        public QuicStream Stream { get; init; }
        [JsonIgnore]
        public QuicConnection Connection { get; init; }

        [JsonIgnore]
        public Socket QuicUdp { get; init; }
        [JsonIgnore]
        public Socket RemoteUdp { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;
        private readonly ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private readonly LastTicksManager pingTicks = new();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");


        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cts = new CancellationTokenSource();
            _ = ProcessWrite();

            _ = ProcessHeart();

        }
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        private async Task ProcessWrite()
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent((1 << BufferSize) * 1024);
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    int length = await Stream.ReadAsync(buffer.Memory, cts.Token).ConfigureAwait(false);
                    if (length == 0)
                    {
                        break;
                    }
                    await ReadPacket(buffer.Memory.Slice(0, length)).ConfigureAwait(false);
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
        private async Task ReadPacket(Memory<byte> buffer)
        {
            //是一个完整的包
            if (bufferCache.Size == 0 && buffer.Length > 4)
            {
                int packageLen = buffer.Span.ToInt32();
                if (packageLen + 4 <= buffer.Length)
                {
                    await CallbackPacket(buffer.Slice(4, packageLen)).ConfigureAwait(false);
                    buffer = buffer.Slice(4 + packageLen);
                }
                if (buffer.Length == 0)
                    return;
            }

            bufferCache.AddRange(buffer);
            do
            {
                int packageLen = bufferCache.Data.Span.ToInt32();
                if (packageLen + 4 > bufferCache.Size)
                {
                    break;
                }
                await CallbackPacket(bufferCache.Data.Slice(4, packageLen)).ConfigureAwait(false);


                bufferCache.RemoveRange(0, packageLen + 4);
            } while (bufferCache.Size > 4);
        }
        private async Task CallbackPacket(Memory<byte> packet)
        {
            ReceiveBytes += packet.Length;
            LastTicks.Update();
            if (packet.Length == pingBytes.Length && packet.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
            {
                if (packet.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes).ConfigureAwait(false);
                }
                else if (packet.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)pingTicks.Diff();
                }
            }
            else
            {
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
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    if (LastTicks.DiffGreater(3000))
                    {
                        pingTicks.Update();
                        await SendPingPong(pingBytes).ConfigureAwait(false);
                    }
                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + data.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData.AsSpan());
            data.AsMemory().CopyTo(heartData.AsMemory(4));
            SendBytes += data.Length;

            await semaphoreSlim.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                await Stream.WriteAsync(heartData.AsMemory(0, length), cts.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Dispose();
            }
            finally
            {
                semaphoreSlim.Release();
            }

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);


        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                await Stream.WriteAsync(data, cts.Token).ConfigureAwait(false);

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
                semaphoreSlim.Release();
            }
            return false;
        }
        public async Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return await SendAsync(buffer.AsMemory(offset, length)).ConfigureAwait(false);
        }

#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        public void Dispose()
        {
            LastTicks.Clear();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");

            callback?.Closed(this, userToken);
            callback = null;
            userToken = null;
            cts?.Cancel();
            bufferCache?.Clear(true);

            Stream?.Close();
            Stream?.Dispose();
            Connection?.CloseAsync(0x0a);
            Connection?.DisposeAsync();
            QuicUdp?.SafeClose();
            RemoteUdp?.SafeClose();

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
