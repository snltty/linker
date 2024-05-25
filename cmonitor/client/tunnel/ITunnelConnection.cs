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

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool byFrame = true);

        public void Close();

        public string ToString();
    }

    public sealed class TunnelConnectionTcp : ITunnelConnection
    {
        public string RemoteMachineName { get; init; }

        public string TransactionId { get; init; }

        public string TransportName { get; init; }

        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public TunnelDirection Direction { get; init; }

        public IPEndPoint IPEndPoint { get; init; }

        public bool Connected => Socket != null && Socket.CanWrite;

        [JsonIgnore]
        public SslStream Socket { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool byFrame;
        private Pipe pipe;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">数据回调</param>
        /// <param name="userToken">自定义数据</param>
        /// <param name="byFrame">是否处理粘包，true时，请在首部4字节标注数据长度</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken, bool byFrame = true)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;
            this.byFrame = byFrame;
            cancellationTokenSource = new CancellationTokenSource();
            pipe = new Pipe();

            _ = ProcessWrite();
            _ = ProcessReader();
        }
        private async Task ProcessWrite()
        {
            var writer = pipe.Writer;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    Memory<byte> buffer = writer.GetMemory(8 * 1024);
                    int length = await Socket.ReadAsync(buffer, cancellationTokenSource.Token);
                    if (length == 0)
                    {
                        Cancel();
                        break;
                    }
                    writer.Advance(length);
                    await writer.FlushAsync();
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
                writer.Complete();
            }
        }
        private async Task ProcessReader()
        {
            PipeReader reader = pipe.Reader;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    ReadResult readResult = await reader.ReadAsync().ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    if (buffer.Length == 0)
                    {
                        Cancel();
                        break;
                    }
                    SequencePosition end = await ReadPacket(buffer).ConfigureAwait(false);
                    reader.AdvanceTo(end);
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
                bufferCache.Clear(true);
                reader.Complete();
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
            if (byFrame == false)
            {
                SequencePosition position = buffer.Start;
                if (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
                }
                return buffer.End;
            }

            //粘包
            while (buffer.Length > 4)
            {
                int length = ReaderHead(buffer);
                if (buffer.Length < length + 4)
                {
                    break;
                }

                ReadOnlySequence<byte> cache = buffer.Slice(4, length);
                SequencePosition position = cache.Start;
                while (cache.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    bufferCache.AddRange(memory);
                }
                await callback.Receive(this, bufferCache.Data.Slice(0, bufferCache.Size), this.userToken).ConfigureAwait(false);
                bufferCache.Clear();

                SequencePosition endPosition = buffer.GetPosition(4 + length);
                buffer = buffer.Slice(endPosition);
            }
            return buffer.Start;
        }

        public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            await Socket.WriteAsync(data, cancellationToken);
        }

        private void Cancel()
        {
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            pipe = null;
            bufferCache.Clear(true);
        }
        public void Close()
        {

            Cancel();
            Socket?.Close();
            Socket?.Dispose();
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineName:{RemoteMachineName}";
        }
    }


}
