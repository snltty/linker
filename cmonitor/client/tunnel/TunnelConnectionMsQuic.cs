using common.libs.extends;
using common.libs;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Quic;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace cmonitor.client.tunnel
{
    public sealed class TunnelConnectionMsQuic : ITunnelConnection
    {
        public TunnelConnectionMsQuic()
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

        [JsonIgnore]
        public QuicStream Stream { get; init; }
        [JsonIgnore]
        public QuicConnection Connection { get; init; }

        [JsonIgnore]
        public UdpClient LocalUdp { get; init; }
        [JsonIgnore]
        public UdpClient remoteUdp { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private Pipe pipe;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private long ticks = Environment.TickCount64;

        private byte[] heartBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.msquic.ping");

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
            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1 * 1024 * 1024, resumeWriterThreshold: 128 * 1024));
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
                await writer.CompleteAsync();
                Close();

                Logger.Instance.Error($"tunnel connection writer offline {ToString()}");
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
                await reader.CompleteAsync();
                Close();
                Logger.Instance.Error($"tunnel connection reader offline {ToString()}");
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
                if ((length == heartBytes.Length && packet.Span.SequenceEqual(heartBytes)) == false)
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
                byte[] heartData = new byte[4 + heartBytes.Length];
                heartBytes.Length.ToBytes(heartData);
                heartBytes.AsMemory().CopyTo(heartData.AsMemory(4));

                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (Environment.TickCount64 - ticks > 3000)
                    {
                        await SendAsync(heartData);
                    }
                    await Task.Delay(3000);
                }
            }
            catch (Exception)
            {
            }
        }


        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await Stream.WriteAsync(data, cancellationTokenSource.Token);
                ticks = Environment.TickCount64;
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
                semaphoreSlim.Release();
            }
        }

        public void Close()
        {
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            pipe = null;
            bufferCache.Clear(true);

            Stream?.Close();
            Stream?.Dispose();

            Connection?.CloseAsync(0x0a);
            Connection?.DisposeAsync();

            LocalUdp?.Close();
            remoteUdp?.Close();
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineName:{RemoteMachineName}";
        }
    }
}
