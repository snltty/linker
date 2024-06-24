using Linker.Libs;
using Linker.Libs.Extends;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Linker.Server
{
    public interface IConnectionReceiveCallback
    {
        public Task Receive(IConnection connection, ReadOnlyMemory<byte> data, object state);
    }

    public interface IConnection
    {
        /// <summary>
        /// 连接id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 链接名
        /// </summary>
        public string Name { get; set; }
        public bool Connected { get; }

        /// <summary>
        /// 外网IP
        /// </summary>
        public IPEndPoint Address { get; }
        /// <summary>
        /// 内网IP
        /// </summary>
        public IPEndPoint LocalAddress { get; }

        /// <summary>
        /// 你的ssl流
        /// </summary>
        public SslStream SourceStream { get; }
        /// <summary>
        /// 你的socket
        /// </summary>
        public Socket SourceSocket { get; }
        /// <summary>
        /// 你的网络流
        /// </summary>
        public NetworkStream SourceNetworkStream { get; }
        /// <summary>
        /// 对方的网络流
        /// </summary>
        public SslStream TargetStream { get; set; }
        /// <summary>
        /// 对方的socket
        /// </summary>
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 对方的网络流
        /// </summary>
        public NetworkStream TargetNetworkStream { get; set; }

        /// <summary>
        /// 延迟ms
        /// </summary>
        public int Delay { get; }
        /// <summary>
        /// 已发送字节数
        /// </summary>
        public long SendBytes { get; }
        /// <summary>
        /// 已接收字节数
        /// </summary>
        public long ReceiveBytes { get; }
        /// <summary>
        /// 中继限速 byte/s
        /// </summary>
        public uint RelayLimit { get; set; }

        #region 接收数据
        public MessageRequestWrap ReceiveRequestWrap { get; }
        public MessageResponseWrap ReceiveResponseWrap { get; }
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        /// <summary>
        /// 开始就接收数据
        /// </summary>
        /// <param name="callback">处理回调</param>
        /// <param name="userToken">携带自定义数据，回调时带过去</param>
        /// <param name="byFrame">是否处理粘包</param>
        public void BeginReceive(IConnectionReceiveCallback callback, object userToken, bool byFrame = true);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<bool> SendAsync(ReadOnlyMemory<byte> data);
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task<bool> SendAsync(byte[] data, int length);

        /// <summary>
        /// 开始中继，不再处理包，在此之前，应该先调用Cancel取消处理包，在等待一段时间后，再开始中继
        /// </summary>
        /// <returns></returns>
        public Task RelayAsync();

        /// <summary>
        /// 取消处理包
        /// </summary>
        public void Cancel();
        public void Disponse(int value = 0);

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

        public string Id { get; set; }
        public string Name { get; set; }
        public virtual bool Connected => false;
        public IPEndPoint Address { get; protected set; }
        public IPEndPoint LocalAddress { get; protected set; }

        public SslStream SourceStream { get; protected set; }
        public Socket SourceSocket { get; protected set; }
        public NetworkStream SourceNetworkStream { get; set; }
        public SslStream TargetStream { get; set; }
        public Socket TargetSocket { get; set; }
        public NetworkStream TargetNetworkStream { get; set; }

        public int Delay { get; protected set; }
        public long SendBytes { get; protected set; }
        public long ReceiveBytes { get; protected set; }
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
            length = 0;
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
        public abstract Task RelayAsync();


        public virtual void Cancel()
        {
        }
        public virtual void Disponse(int value = 0)
        {
        }
    }

    public sealed class TcpConnection : Connection
    {
        public TcpConnection(SslStream stream, NetworkStream networkStream, Socket socket, IPEndPoint local, IPEndPoint remote) : base()
        {
            SourceStream = stream;
            SourceNetworkStream = networkStream;
            SourceSocket = socket;
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

        public override bool Connected => SourceSocket != null && Environment.TickCount64 - ticks < 15000;

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
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private long ticks = Environment.TickCount64;
        private long pingStart = Environment.TickCount64;
        private static byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private static byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");
        private bool pong = true;

        public override void BeginReceive(IConnectionReceiveCallback callback, object userToken, bool framing = true)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;
            this.framing = framing;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceWrite = new CancellationTokenSource();

            _ = ProcessWrite();
            _ = ProcessHeart();
        }
        private async Task ProcessWrite()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int length = 0;
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (SourceStream != null)
                    {
                        length = await SourceStream.ReadAsync(buffer, cancellationTokenSource.Token);
                    }
                    else
                    {
                        length = await SourceSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationTokenSource.Token);
                    }
                    if (length == 0)
                    {
                        Disponse(1);
                        break;
                    }
                    ReceiveBytes += length;
                    ticks = Environment.TickCount64;
                    await ReadPacket(buffer.AsMemory(0, length));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Disponse(2);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                //SourceStream?.Close();
                //SourceStream?.Dispose();
                // TargetStream?.Close();
                //TargetStream?.Dispose();
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
            if (packet.Length == pingBytes.Length && (packet.Span.SequenceEqual(pingBytes) || packet.Span.SequenceEqual(pongBytes)))
            {
                if (packet.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes);
                }
                else if (packet.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)(Environment.TickCount64 - pingStart);
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

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + pingBytes.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData);
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            await semaphoreSlim.WaitAsync();
            try
            {
                if (SourceStream != null)
                {
                    await SourceStream.WriteAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token);
                }
                else
                {
                    await SourceSocket.SendAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token);
                }

            }
            catch (Exception)
            {
                pong = true;
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
            pingStart = Environment.TickCount64;
            await SendPingPong(pingBytes);
        }
        public override async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (SourceStream != null) await semaphoreSlim.WaitAsync();
            try
            {
                if (SourceStream != null)
                    await SourceStream.WriteAsync(data, cancellationTokenSourceWrite.Token);
                else
                    await SourceSocket.SendAsync(data, cancellationTokenSourceWrite.Token);
                SendBytes += data.Length;
                ticks = Environment.TickCount64;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Disponse(3);
            }
            finally
            {
                if (SourceStream != null) semaphoreSlim.Release();
            }
            return true;
        }
        public override async Task<bool> SendAsync(byte[] data, int length)
        {
            return await SendAsync(data.AsMemory(0, length));
        }

        public override async Task RelayAsync()
        {
            if (TargetNetworkStream != null)
            {
                await CopyToAsync(SourceNetworkStream, TargetNetworkStream);
            }
        }
        private async Task CopyToAsync(NetworkStream source, NetworkStream destination)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
                {
                    if (RelayLimit > 0)
                    {
                        int length = bytesRead;
                        TryLimit(ref length);
                        while (length > 0)
                        {
                            await Task.Delay(30);
                            TryLimit(ref length);
                        }
                    }
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Disponse(4);
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
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            bufferCache.Clear(true);

        }
        public override void Disponse(int value = 0)
        {
            Cancel();
            cancellationTokenSourceWrite?.Cancel();
            base.Disponse();

            try
            {
                SourceNetworkStream?.Close();
                SourceNetworkStream?.Dispose();
                TargetNetworkStream?.Close();
                TargetNetworkStream?.Dispose();
            }
            catch (Exception)
            {
            }
            SourceSocket?.SafeClose();
            TargetSocket?.SafeClose();
        }
    }

}
