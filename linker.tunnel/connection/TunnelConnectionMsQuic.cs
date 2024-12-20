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

        public bool Connected => Stream != null && Stream.CanWrite && LastTicks.HasValue();
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        [JsonIgnore]
        public QuicStream Stream { get; init; }
        [JsonIgnore]
        public QuicConnection Connection { get; init; }

        [JsonIgnore]
        public Socket QuicUdp { get; init; }
        [JsonIgnore]
        public Socket RemoteUdp { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private LastTicksManager pingTicks = new();
        private byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");
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
            this.framing = framing;

            cancellationTokenSource = new CancellationTokenSource();
            _ = ProcessWrite();
            _ = ProcessHeart();

        }
        private async Task ProcessWrite()
        {
            byte[] buffer = new byte[(1 << BufferSize) * 1024];
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    int length = await Stream.ReadAsync(buffer, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (length == 0)
                    {
                        break;
                    }
                    await ReadPacket(buffer.AsMemory(0, length)).ConfigureAwait(false);
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
            if (framing == false)
            {
                await CallbackPacket(buffer).ConfigureAwait(false);
                return;
            }

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
            if (packet.Length == pingBytes.Length && (packet.Span.SequenceEqual(pingBytes) || packet.Span.SequenceEqual(pongBytes)))
            {
                if (packet.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes).ConfigureAwait(false);
                }
                else if (packet.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)pingTicks.Diff();
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
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + data.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData);
            data.AsMemory().CopyTo(heartData.AsMemory(4));
            SendBytes += data.Length;

            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                await Stream.WriteAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                pong = true;
                Dispose();
            }
            finally
            {
                semaphoreSlim.Release();
            }

            ArrayPool<byte>.Shared.Return(heartData);
        }


        public async Task SendPing()
        {
            if (pong == false) return;
            pong = false;
            pingTicks.Update();
            await SendPingPong(pingBytes).ConfigureAwait(false);
        }
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                await Stream.WriteAsync(data, cancellationTokenSource.Token).ConfigureAwait(false);
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


        public void Dispose()
        {
            LastTicks.Clear();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");

            callback?.Closed(this, userToken);
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            bufferCache?.Clear(true);

            Stream?.Close();
            Stream?.Dispose();
            Connection?.CloseAsync(0x0a);
            Connection?.DisposeAsync();
            QuicUdp?.SafeClose();
            RemoteUdp?.SafeClose();

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
