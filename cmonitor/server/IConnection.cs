using common.libs;
using common.libs.extends;
using System;
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

        public SslStream SourceStream { get; }
        public SslStream TargetStream { get; set; }

        public uint RelayLimit { get; set; }

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

        public SslStream SourceStream { get; protected set; }
        public SslStream TargetStream { get; set; }


        public virtual uint RelayLimit { get; set; } = 0;

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
            SourceStream = stream;

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

        }

        public override bool Connected => SourceStream != null && SourceStream.CanWrite;

        private uint relayLimit = 0;
        private double relayLimitToken = 0;
        public override uint RelayLimit
        {
            get => relayLimit; set
            {
                relayLimit = value;
                relayLimitToken = relayLimit / 1000.0;
                relayLimitBucket = relayLimit;
            }
        }

        private IConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationTokenSource cancellationTokenSourceWrite;
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
            cancellationTokenSourceWrite = new CancellationTokenSource();
            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 512 * 1024, resumeWriterThreshold: 64 * 1024));

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
                    int length = await SourceStream.ReadAsync(buffer, cancellationTokenSource.Token);
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
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                if (SourceStream.CanRead == false)
                    Disponse();
            }
            finally
            {
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
                    if (buffer.Length == 0 || readResult.IsCompleted || readResult.IsCanceled)
                    {
                        break;
                    }
                    SequencePosition end = await ReadPacket(buffer).ConfigureAwait(false);
                    reader.AdvanceTo(end);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                if (SourceStream.CanRead == false)
                    Disponse();
            }
            finally
            {
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
            if (TargetStream != null)
            {
                foreach (var memory in buffer)
                {
                    await TargetStream.WriteAsync(memory).ConfigureAwait(false);
                }

                Cancel();
                _ = CopyToAsync(SourceStream, TargetStream);

                return buffer.End;
            }
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
                int length = ReaderHead(buffer);
                if (buffer.Length < length + 4)
                {
                    break;
                }

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

                buffer = buffer.Slice(4 + length);
            }
            return buffer.Start;
        }


        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public override async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await SourceStream.WriteAsync(data, cancellationTokenSourceWrite.Token);
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
            return true;
        }
        public override async Task<bool> SendAsync(byte[] data, int length)
        {
            return await SendAsync(data.AsMemory(0, length));
        }

        private async Task CopyToAsync(SslStream source, SslStream destination)
        {
            await Task.Delay(500);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer)).ConfigureAwait(false)) != 0)
                {
                    /*
                    int length = bytesRead;
                    TryLimit(ref length);
                    while (length > 0)
                    {
                        await Task.Delay(30);
                        TryLimit(ref length);
                    }
                    */
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead)).ConfigureAwait(false);
                    destination.Flush();
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
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private double relayLimitBucket = 0;
        private long relayLimitTicks = Environment.TickCount64;
        private void TryLimit(ref int length)
        {
            long _relayLimitTicks = Environment.TickCount64;
            long relayLimitTicksTemp = _relayLimitTicks - relayLimitTicks;
            relayLimitTicks = _relayLimitTicks;
            relayLimitBucket += relayLimitTicksTemp * relayLimitToken;
            if (relayLimitBucket > relayLimit) relayLimitBucket = relayLimit;

            if (relayLimitBucket >= length)
            {
                relayLimitBucket -= length;
                length = 0;
            }
            else
            {
                length -= (int)relayLimitBucket;
                relayLimitBucket = 0;
            }
        }


        public override void Cancel()
        {
            pipe.Writer.Complete();
            pipe.Reader.Complete();
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();

            pipe = null;

            bufferCache.Clear(true);
        }
        public override void Disponse()
        {
            Cancel();
            cancellationTokenSourceWrite?.Cancel();
            base.Disponse();
            try
            {
                if (SourceStream != null)
                {
                    SourceStream.ShutdownAsync();
                    SourceStream.Dispose();

                    TargetStream?.ShutdownAsync();
                    TargetStream?.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }
    }

}
