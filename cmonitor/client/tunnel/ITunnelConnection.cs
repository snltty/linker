using common.libs;
using common.libs.extends;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Text.Json.Serialization;

namespace cmonitor.client.tunnel
{
    public enum TunnelProtocolType : byte
    {
        Tcp = 1,
        Udp = 2,
        Quic = 4,
    }
    public enum TunnelMode : byte
    {
        Client = 0,
        Server = 1,
    }
    public enum TunnelType : byte
    {
        P2P = 0,
        Relay = 1,
    }
    public enum TunnelDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public interface ITunnelConnectionReceiveCallback
    {
        public Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state);
        public Task Closed(ITunnelConnection connection, object state);
    }

    public interface ITunnelConnection
    {
        public string RemoteMachineName { get; }
        public string TransactionId { get; }
        public string TransportName { get; }
        public string Label { get; }
        public TunnelMode Mode { get; }
        public TunnelType Type { get; }
        public TunnelProtocolType ProtocolType { get; }
        public TunnelDirection Direction { get; }
        public IPEndPoint IPEndPoint { get; }

        public bool Connected { get; }

        public Task SendAsync(ReadOnlyMemory<byte> data);
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool framing = true);

        public void Close();

        public string ToString();
    }

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

        [JsonIgnore]
        public SslStream Stream { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private Pipe pipe;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

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
            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1 * 1024 * 1024, resumeWriterThreshold: 128 * 1024));
            _ = ProcessWrite();
            _ = ProcessReader();

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

                try
                {
                    await callback.Receive(this, bufferCache.Data.Slice(0, bufferCache.Size), this.userToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                bufferCache.Clear();

                //分割去掉已使用的数据
                buffer = buffer.Slice(4 + length);
            }
            return buffer.Start;
        }


        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await Stream.WriteAsync(data, cancellationTokenSource.Token);
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
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineName:{RemoteMachineName}";
        }
    }

}
