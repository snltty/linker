using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Net.Security;
using System.Net;
using System.Text.Json.Serialization;
using System.Text;
using System.Net.Sockets;

namespace linker.tunnel.connection
{
    public sealed class TunnelConnectionTcp : ITunnelConnection
    {
        public TunnelConnectionTcp()
        {
        }

        public string RemoteMachineId { get; init; }
        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public string TransportName { get; init; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }
        public bool SSL { get; init; }
        public byte BufferSize { get; init; } = 3;
        public bool Connected => Socket != null && LastTicks.Timeout(15000) == false;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        [JsonIgnore]
        public SslStream Stream { get; init; }

        [JsonIgnore]
        public Socket Socket { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;
        private bool framing;
        private ReceiveDataBuffer bufferCache = new ReceiveDataBuffer();

        private LastTicksManager pingTicks = new LastTicksManager();
        private byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ping");
        private byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.pong");
        private bool pong = true;

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
            _ = ProcessWrite();
            _ = ProcessHeart();
        }

        private async Task ProcessWrite()
        {
            byte[] buffer = new byte[(1 << BufferSize) * 1024];
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
                //ArrayPool<byte>.Shared.Return(buffer);
                Dispose();
            }
        }
        private async Task ReadPacket(Memory<byte> buffer)
        {
            //不分包
            if (framing == false)
            {
                await CallbackPacket(buffer).ConfigureAwait(false);
                return;
            }

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
            if (packet.Length == pingBytes.Length)
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
                return;
            }
            try
            {
                await callback.Receive(this, packet, this.userToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
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

                    if (LastTicks.Greater(3000))
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


        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        public async Task SendPing()
        {
            if (pong == false) return;
            pong = false;
            pingTicks.Update();
            await SendPingPong(pingBytes).ConfigureAwait(false);
        }
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (callback == null) return false;

            if (Stream != null)
                await semaphoreSlim.WaitAsync().ConfigureAwait(false);
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
        }

        public override string ToString()
        {
            return this.ToJson();
        }
        public bool Equals(ITunnelConnection connection)
        {
            return connection != null && GetHashCode() == connection.GetHashCode() && IPEndPoint.Equals(connection.IPEndPoint);
        }
    }
}
