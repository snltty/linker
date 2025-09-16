using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace linker.messenger
{
    /// <summary>
    /// 服务器连接对象接收数据回调
    /// </summary>
    public interface IConnectionReceiveCallback
    {
        public Task Receive(IConnection connection, ReadOnlyMemory<byte> data, object state);
    }

    /// <summary>
    /// 服务器连接对象
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// 连接id，存的应该是客户端id
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
        [JsonIgnore]
        public SslStream SourceStream { get; }
        /// <summary>
        /// 你的socket
        /// </summary>
        [JsonIgnore]
        public Socket SourceSocket { get; }
        /// <summary>
        /// 你的网络流
        /// </summary>
        [JsonIgnore]
        public NetworkStream SourceNetworkStream { get; }
        /// <summary>
        /// 对方的网络流
        /// </summary>
        [JsonIgnore]
        public SslStream TargetStream { get; set; }
        /// <summary>
        /// 对方的socket
        /// </summary>
        [JsonIgnore]
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 对方的网络流
        /// </summary>
        [JsonIgnore]
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

        #region 接收数据
        [JsonIgnore]
        public MessageRequestWrap ReceiveRequestWrap { get; }
        [JsonIgnore]
        public MessageResponseWrap ReceiveResponseWrap { get; }
        [JsonIgnore]
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

        public void Disponse(int value = 0);

        #region 回复消息相关

        [JsonIgnore]
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

        #region 接收数据
        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        public MessageResponseWrap ReceiveResponseWrap { get; set; }
        public ReadOnlyMemory<byte> ReceiveData { get; set; }
        #endregion

        #region 回复数据
        public Memory<byte> ResponseData { get; private set; }
        private byte[] responseData;
        private int length;

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
            num.ToBytes(responseData.AsSpan());
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
            str.Length.ToBytes(responseData.AsSpan());
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

        public virtual void Disponse(int value = 0)
        {
        }
    }

    /// <summary>
    /// tcp连接对象
    /// </summary>
    public sealed class TcpConnection : Connection
    {
        public TcpConnection(SslStream stream, NetworkStream networkStream, Socket socket, IPEndPoint local, IPEndPoint remote) : base()
        {
            SourceStream = stream;
            SourceNetworkStream = networkStream;
            SourceSocket = socket;
            Address = NetworkHelper.TransEndpointFamily(remote) ;
            LocalAddress = NetworkHelper.TransEndpointFamily(local);

        }

        public override bool Connected => SourceSocket != null && lastTicks.Expired(15000) == false;


        private IConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationTokenSource cancellationTokenSourceWrite;
        private object userToken;
        private bool framing;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private LastTicksManager lastTicks = new LastTicksManager();
        private LastTicksManager pingTicks = new LastTicksManager();
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
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(8*1024);
            try
            {
                int length = 0;
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (SourceStream != null)
                    {
                        length = await SourceStream.ReadAsync(buffer.Memory, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        length = await SourceSocket.ReceiveAsync(buffer.Memory, SocketFlags.None, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    if (length == 0)
                    {
                        Disponse(1);
                        break;
                    }
                    ReceiveBytes += length;
                    lastTicks.Update();
                    await ReadPacket(buffer.Memory.Slice(0, length)).ConfigureAwait(false);
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

            }
            finally
            {
                Disponse(2);
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
                    await callback.Receive(this, packet, userToken).ConfigureAwait(false);
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
                    if (lastTicks.DiffGreater(3000))
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

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private async Task SendPingPong(byte[] data)
        {
            int length = 4 + pingBytes.Length;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(length);
            data.Length.ToBytes(heartData.AsSpan());
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (SourceStream != null)
                {
                    await SourceStream.WriteAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    await SourceSocket.SendAsync(heartData.AsMemory(0, length), cancellationTokenSource.Token).ConfigureAwait(false);
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
            pingTicks.Update();
            await SendPingPong(pingBytes).ConfigureAwait(false);
        }
        public override async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (SourceStream != null) await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (SourceStream != null)
                    await SourceStream.WriteAsync(data, cancellationTokenSourceWrite.Token).ConfigureAwait(false);
                else
                    await SourceSocket.SendAsync(data, cancellationTokenSourceWrite.Token).ConfigureAwait(false);
                SendBytes += data.Length;
                lastTicks.Update();
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
            return await SendAsync(data.AsMemory(0, length)).ConfigureAwait(false);
        }

        public override void Disponse(int value = 0)
        {
            callback = null;
            userToken = null;
            cancellationTokenSource?.Cancel();
            bufferCache.Clear(true);

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

            lastTicks.Clear();
        }
    }

}
