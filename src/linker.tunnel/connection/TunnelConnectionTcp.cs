using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net.Security;
using System.Net;
using System.Text.Json.Serialization;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;

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
        public bool Connected => Socket != null && LastTicks.Expired(15000) == false;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        [JsonIgnore]
        public byte[] SendBuffer { get; set; } = Helper.EmptyArray;


        [JsonIgnore]
        public SslStream Stream { get; init; }

        [JsonIgnore]
        public Socket Socket { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private readonly ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");

        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">数据回调</param>
        /// <param name="userToken">自定义数据</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
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
            byte[] buffer = new byte[16 * 1024];
            try
            {
                int length = 0;
                while (cancellationTokenSource.IsCancellationRequested == false)
                {

                    if (Stream != null)
                    {
                        length = await Stream.ReadAsync(buffer).ConfigureAwait(false);
                        if (length == 0) break;

                        await ReadPacket(buffer.AsMemory(0, length)).ConfigureAwait(false);
                    }
                    else
                    {
                        length = await Socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                        if (length == 0) break;
                        await ReadPacket(buffer.AsMemory(0, length)).ConfigureAwait(false);

                        while (Socket.Available > 0)
                        {
                            length = Socket.Receive(buffer);
                            if (length == 0) break;
                            await ReadPacket(buffer.AsMemory(0, length)).ConfigureAwait(false);
                        }
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
        private async Task ReadPacket(Memory<byte> buffer)
        {
            //没有缓存，可能是一个完整的包
            if (bufferCache.Size == 0 && buffer.Length > 4)
            {
                int packageLen = buffer.Span.ToInt32();
                //数据足够，包长度+4，那就存在一个完整包 
                if (packageLen + 4 <= buffer.Length)
                {
                    await CallbackPacket(buffer.Slice(4, packageLen)).ConfigureAwait(false);
                    buffer = buffer.Slice(4 + packageLen);
                }
                //没有剩下的数据就不继续往下了
                if (buffer.Length == 0)
                    return;
            }
            //添加到缓存
            bufferCache.AddRange(buffer);
            do
            {
                //取出一个一个包
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
                while (cancellationTokenSource.IsCancellationRequested == false)
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

            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Stream != null)
                {

                    await Stream.WriteAsync(heartData.AsMemory(0, length)).ConfigureAwait(false);
                }
                else
                {
                    await Socket.SendAsync(heartData.AsMemory(0, length)).ConfigureAwait(false);
                }
                SendBytes += data.Length;
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

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (callback == null) return false;

            if (pipe != null)
            {
                Memory<byte> memory = pipe.Writer.GetMemory(data.Length);
                data.CopyTo(memory);
                pipe.Writer.Advance(data.Length);
                await pipe.Writer.FlushAsync();
                return true;
            }

            if (Stream != null)
            {
                await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            }
            try
            {
                if (Stream != null)
                {
                    await Stream.WriteAsync(data).ConfigureAwait(false);
                }
                else
                {
                    await Socket.SendAsync(data, SocketFlags.None).ConfigureAwait(false);
                }
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
                if (Stream != null)
                    semaphoreSlim.Release();
            }
            return false;
        }
        public async Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            if (callback == null) return false;

            if (pipe != null)
            {
                ReadOnlyMemory<byte> data = buffer.AsMemory(offset, length);
                Memory<byte> memory = pipe.Writer.GetMemory(data.Length);
                data.CopyTo(memory);
                pipe.Writer.Advance(data.Length);
                await pipe.Writer.FlushAsync();
                return true;
            }

            if (Stream != null)
            {
                await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            }
            try
            {
                if (Stream != null)
                {
                    await Stream.WriteAsync(buffer.AsMemory(offset, length)).ConfigureAwait(false);
                }
                else
                {
                    await Socket.SendAsync(buffer.AsMemory(offset, length), SocketFlags.None).ConfigureAwait(false);
                }
                SendBytes += length;
                LastTicks.Update();
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
                if (Stream != null)
                    semaphoreSlim.Release();
            }
            return false;
        }


        private Pipe pipe;
        public void StartPacketMerge()
        {
            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 800 * 1024));
            _ = Reader();
        }
        private async Task Reader()
        {
            byte[] pipeBuffer = new byte[10 * 1024];
            while (cancellationTokenSource.IsCancellationRequested == false)
            {
                ReadResult result = await pipe.Reader.ReadAsync();
                if (result.IsCompleted && result.Buffer.IsEmpty)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }

                ReadOnlySequence<byte> buffer = result.Buffer;
                while (buffer.Length > 0)
                {
                    int chunkSize = (int)Math.Min(buffer.Length, 8192);
                    ReadOnlySequence<byte> chunk = buffer.Slice(0, chunkSize);

                    if (Stream != null) await semaphoreSlim.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        chunk.CopyTo(pipeBuffer);
                        if (Stream != null)
                        {
                            await Stream.WriteAsync(pipeBuffer.AsMemory(0, chunkSize)).ConfigureAwait(false);
                        }
                        else
                        {
                            await Socket.SendAsync(pipeBuffer.AsMemory(0, chunkSize), SocketFlags.None).ConfigureAwait(false);
                        }
                        SendBytes += chunk.Length;
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
                        if (Stream != null) semaphoreSlim.Release();
                    }

                    buffer = buffer.Slice(chunkSize);

                }
                pipe.Reader.AdvanceTo(result.Buffer.End);

            }
        }

        public void Dispose()
        {
            LastTicks.Clear();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            callback?.Closed(this, userToken);
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();

            bufferCache.Clear(true);

            Stream?.Close();
            Stream?.Dispose();

            Socket?.SafeClose();

            try
            {
                pipe?.Writer.Complete();
                pipe?.Reader.Complete();
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
