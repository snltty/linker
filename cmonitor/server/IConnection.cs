using common.libs;
using common.libs.extends;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace cmonitor.server
{
    public interface IConnectionReceiveCallback
    {
        public Task Receive(IConnection connection, ReadOnlyMemory<byte> data, object state);
    }

    public interface IConnection
    {
        public string Name { get; set; }
        public bool Connected { get; }

        public IPEndPoint Address { get; }
        public IPEndPoint LocalAddress { get; }

        public SslStream TcpSourceSocket { get; }
        public SslStream TcpTargetSocket { get; set; }

        #region 接收数据
        public MessageRequestWrap ReceiveRequestWrap { get; }
        public MessageResponseWrap ReceiveResponseWrap { get; }
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        public void BeginReceive(IConnectionReceiveCallback callback, object userToken, bool byFrame = true);

        public Task<bool> SendAsync(ReadOnlyMemory<byte> data);
        public Task<bool> SendAsync(byte[] data, int length);

        public void Cancel();
        public void Disponse();

        #region 回复消息相关

        public Memory<byte> ResponseData { get; }
        public void Write(Memory<byte> data);
        public void Write(ulong num);
        public void Write(ushort num);
        public void Write(ushort[] nums);
        public void WriteUTF8(string str);
        public void WriteUTF16(string str);
        public void Return();
        #endregion

    }

    public abstract class Connection : IConnection
    {
        public Connection()
        {
        }

        public string Name { get; set; }
        public virtual bool Connected => false;
        public IPEndPoint Address { get; protected set; }
        public IPEndPoint LocalAddress { get; protected set; }

        public SslStream TcpSourceSocket { get; protected set; }
        public SslStream TcpTargetSocket { get; set; }


        #region 接收数据
        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        public MessageResponseWrap ReceiveResponseWrap { get; set; }
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        #region 回复数据
        public Memory<byte> ResponseData { get; private set; }
        private byte[] responseData;
        private int length = 0;

        public void Write(Memory<byte> data)
        {
            ResponseData = data;
        }
        public void Write(ulong num)
        {
            length = 8;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort num)
        {
            length = 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            num.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }
        public void Write(ushort[] nums)
        {
            length = nums.Length * 2;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            nums.ToBytes(responseData);
            ResponseData = responseData.AsMemory(0, length);
        }

        public void WriteUTF8(string str)
        {
            var span = str.AsSpan();
            responseData = ArrayPool<byte>.Shared.Rent((span.Length + 1) * 3 + 8);
            var memory = responseData.AsMemory();

            int utf8Length = span.ToUTF8Bytes(memory.Slice(8));
            span.Length.ToBytes(memory);
            utf8Length.ToBytes(memory.Slice(4));
            length = utf8Length + 8;

            ResponseData = responseData.AsMemory(0, length);
        }

        public void WriteUTF16(string str)
        {
            var span = str.GetUTF16Bytes();
            length = span.Length + 4;
            responseData = ArrayPool<byte>.Shared.Rent(length);
            str.Length.ToBytes(responseData);
            span.CopyTo(responseData.AsSpan(4));

            ResponseData = responseData.AsMemory(0, length);
        }

        public void Return()
        {
            if (length > 0 && ResponseData.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(responseData);
            }
            ResponseData = Helper.EmptyArray;
            responseData = null;
            length = 0;
        }
        #endregion


        public abstract void BeginReceive(IConnectionReceiveCallback callback, object userToken, bool byFrame = true);

        public abstract Task<bool> SendAsync(ReadOnlyMemory<byte> data);
        public abstract Task<bool> SendAsync(byte[] data, int length);

        public virtual void Cancel()
        {
        }
        public virtual void Disponse()
        {
        }
    }

    public sealed class TcpConnection : Connection
    {
        public TcpConnection(SslStream stream, IPEndPoint local, IPEndPoint remote) : base()
        {
            TcpSourceSocket = stream;

            if (remote.Address.AddressFamily == AddressFamily.InterNetworkV6 && remote.Address.IsIPv4MappedToIPv6)
            {
                remote = new IPEndPoint(new IPAddress(remote.Address.GetAddressBytes()[^4..]), remote.Port);
            }
            Address = remote;

            if (local.Address.AddressFamily == AddressFamily.InterNetworkV6 && local.Address.IsIPv4MappedToIPv6)
            {
                local = new IPEndPoint(new IPAddress(local.Address.GetAddressBytes()[^4..]), local.Port);
            }
            LocalAddress = local;

            sendCancellationTokenSource = new CancellationTokenSource();
            senderPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1 * 1024 * 1024, resumeWriterThreshold: 128 * 1024));
            _ = ProcessSender();
        }

        public override bool Connected => TcpSourceSocket != null && TcpSourceSocket.CanWrite;

        private IConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private Pipe pipe;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();
        public override void BeginReceive(IConnectionReceiveCallback callback, object userToken, bool framing = true)
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
            var writer = pipe.Writer;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    Memory<byte> buffer = writer.GetMemory(8 * 1024);
                    int length = await TcpSourceSocket.ReadAsync(buffer, cancellationTokenSource.Token);
                    if (length == 0)
                    {
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
                Disponse();
                await writer.CompleteAsync();
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
                Disponse();
                await reader.CompleteAsync();
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
            //已转发
            if (TcpTargetSocket != null)
            {
                SequencePosition position = buffer.Start;
                while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    await TcpTargetSocket.WriteAsync(memory);
                }
                return buffer.End;
            }
            //不分包
            if (framing == false)
            {
                SequencePosition position = buffer.Start;
                while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
                }
                return buffer.End;
            }

            //分包
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


        private CancellationTokenSource sendCancellationTokenSource;
        private Pipe senderPipe;
        private async Task ProcessSender()
        {
            var reader = senderPipe.Reader;
            try
            {
                while (sendCancellationTokenSource.IsCancellationRequested == false)
                {
                    ReadResult readResult = await reader.ReadAsync().ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    if (buffer.Length == 0)
                    {
                        break;
                    }

                    SequencePosition position = buffer.Start;
                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        await TcpSourceSocket.WriteAsync(memory);
                    }

                    reader.AdvanceTo(buffer.End);
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
                Disponse();
                await reader.CompleteAsync();
            }
        }
        public override async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await senderPipe.Writer.WriteAsync(data, sendCancellationTokenSource.Token);
                    await senderPipe.Writer.FlushAsync(sendCancellationTokenSource.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    Disponse();
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Error(ex);
                }
            }
            return false;
        }
        public override async Task<bool> SendAsync(byte[] data, int length)
        {
            return await SendAsync(data.AsMemory(0, length));
        }

        public override void Cancel()
        {
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();

            pipe = null;

            bufferCache.Clear(true);
        }
        public override void Disponse()
        {
            Cancel();
            base.Disponse();
            try
            {
                sendCancellationTokenSource?.Cancel();
                if (TcpSourceSocket != null)
                {
                    TcpSourceSocket.ShutdownAsync();
                    TcpSourceSocket.Dispose();

                    TcpTargetSocket?.ShutdownAsync();
                    TcpTargetSocket?.Dispose();
                }
                senderPipe.Writer.Complete();
                senderPipe.Reader.Complete();
            }
            catch (Exception)
            {
            }
        }
    }

}
