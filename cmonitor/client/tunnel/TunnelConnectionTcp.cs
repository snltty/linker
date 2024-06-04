using common.libs;
using common.libs.extends;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net;
using System.Text.Json.Serialization;
using System.Text;

namespace cmonitor.client.tunnel
{
    public sealed class TunnelConnectionTcp : ITunnelConnection
    {
        public TunnelConnectionTcp()
        {
        }

        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public string TransportName { get; init; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }

        public bool Connected => Stream != null && Stream.CanWrite;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }


        [JsonIgnore]
        public SslStream Stream { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private Pipe pipe;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private long ticks = Environment.TickCount64;
        private long pingStart = Environment.TickCount64;
        private byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");

        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">数据回调</param>
        /// <param name="userToken">自定义数据</param>
        /// <param name="byFrame">是否处理粘包，true时，请在首部4字节标注数据长度</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool framing = true)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;
            this.framing = framing;

            cancellationTokenSource = new CancellationTokenSource();
            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 512 * 1024, resumeWriterThreshold: 64 * 1024));
            _ = ProcessWrite();
            _ = ProcessReader();
            _ = ProcessHeart();
        }
        private async Task ProcessWrite()
        {
            PipeWriter writer = pipe.Writer;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    Memory<byte> buffer = writer.GetMemory(8 * 1024);
                    int length = await Stream.ReadAsync(buffer, cancellationTokenSource.Token);
                    ReceiveBytes += length;
                    ticks = Environment.TickCount64;
                    if (length == 0)
                    {
                        break;
                    }
                    writer.Advance(length);
                    FlushResult result = await writer.FlushAsync();
                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
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
                Close();
            }
        }
        private async Task ProcessReader()
        {
            PipeReader reader = pipe.Reader;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    ReadResult readResult = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    if (buffer.IsEmpty && readResult.IsCompleted)
                    {
                        break;
                    }
                    if (buffer.Length > 0)
                    {
                        SequencePosition end = await ReadPacket(buffer).ConfigureAwait(false);
                        reader.AdvanceTo(end);
                    }
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
                Close();
            }
        }
        private unsafe int ReaderHead(ReadOnlySequence<byte> buffer)
        {
            Span<byte> span = stackalloc byte[4];
            buffer.Slice(0, 4).CopyTo(span);
            return span.ToInt32();
        }
        private async Task<SequencePosition> ReadPacket(ReadOnlySequence<byte> buffer)
        {
            //不分包
            if (framing == false)
            {
                foreach (var memory in buffer)
                {
                    try
                    {
                        await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }
                }
                return buffer.End;
            }

            //分包
            while (buffer.Length > 4)
            {
                //读取头
                int length = ReaderHead(buffer);
                if (buffer.Length < length + 4)
                {
                    break;
                }

                //拼接数据
                ReadOnlySequence<byte> cache = buffer.Slice(4, length);
                foreach (var memory in cache)
                {
                    bufferCache.AddRange(memory);
                }

                Memory<byte> packet = bufferCache.Data.Slice(0, length);
                if (length == pingBytes.Length && (packet.Span.SequenceEqual(pingBytes) || packet.Span.SequenceEqual(pongBytes)))
                {
                    if (packet.Span.SequenceEqual(pingBytes))
                    {
                        await SendPingPong(pongBytes);
                    }
                    else if (packet.Span.SequenceEqual(pongBytes))
                    {
                        Delay = (int)(Environment.TickCount64 - pingStart);
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


                bufferCache.Clear();

                //分割去掉已使用的数据
                buffer = buffer.Slice(4 + length);
            }
            return buffer.Start;
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

            await Stream.WriteAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token);

            ArrayPool<byte>.Shared.Return(heartData);
        }


        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task SendPing()
        {
            await SendPingPong(pingBytes);
        }
        public async Task SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await Stream.WriteAsync(data, cancellationTokenSource.Token);
                SendBytes += data.Length;
                ticks = Environment.TickCount64;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                Close();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void Close()
        {
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            
            bufferCache.Clear(true);

            Stream?.Close();
            Stream?.Dispose();
            try
            {
                pipe?.Writer.Complete();
                pipe?.Reader.Complete();
            }
            catch (Exception)
            {
            }
            pipe = null;
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineName:{RemoteMachineName}";
        }
    }


}
