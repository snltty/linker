using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;
using System.IO.Pipelines;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace linker.tunnel.connection
{
    /// <summary>
    /// UDP隧道
    /// </summary>
    public sealed class TunnelConnectionUdp : ITunnelConnection
    {
        public TunnelConnectionUdp()
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

        public bool Connected => UdpClient != null && LastTicks.Expired(15000) == false;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        private long sendRemaining = 0;
        public long SendBufferRemaining { get => sendRemaining; }
        public long SendBufferFree { get => maxRemaining - sendRemaining; }
        private const long maxRemaining = 128 * 1024;

        public long RecvBufferRemaining { get; }
        public long RecvBufferFree { get => maxRemaining; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        [JsonIgnore]
        public byte[] PacketBuffer { get; set; } = Helper.EmptyArray;

        public bool Receive { get; init; }

        public Socket udpClient;
        [JsonIgnore]
        public Socket UdpClient
        {
            get
            {
                return udpClient;
            }
            init
            {
                udpClient = value;
            }
        }
        [JsonIgnore]
        public ICrypto Crypto { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");
        private readonly byte[] finBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.fing");

        private Pipe pipeSender;
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cts = new CancellationTokenSource();

            _ = Sender();
            if (Receive)
            {
                _ = ProcessWrite();
            }
            _ = ProcessHeart();
        }
        private async Task ProcessWrite()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
            IPEndPoint ep = new IPEndPoint(IPEndPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    SocketReceiveFromResult result = await UdpClient.ReceiveFromAsync(buffer.AsMemory(), ep, cts.Token).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"tunnel connection writer offline 0");
                        continue;
                    }
                    await CallbackPacket(buffer, 0, result.ReceivedBytes).ConfigureAwait(false);
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
                ArrayPool<byte>.Shared.Return(buffer);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {cts.IsCancellationRequested}");
                LoggerHelper.Instance.Error($"tunnel connection disponse 6");
                Dispose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");
            }
        }
        public async Task<bool> ProcessWrite(byte[] buffer, int offset, int length)
        {
            if (callback == null)
            {
                return false;
            }
            await CallbackPacket(buffer, offset, length).ConfigureAwait(false);
            return true;
        }
        private async Task CallbackPacket(byte[] buffer, int offset, int length)
        {
            ReceiveBytes += length;
            LastTicks.Update();

            Memory<byte> memory = buffer.AsMemory(offset, length);
            try
            {
                if (SSL)
                {
                    memory = Crypto.Decode(buffer, offset, length);
                }
                if (memory.Length == pingBytes.Length && memory.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
                {
                    if (memory.Span.SequenceEqual(pingBytes))
                    {
                        await SendPingPong(pongBytes).ConfigureAwait(false);
                    }
                    else if (memory.Span.SequenceEqual(pongBytes))
                    {
                        Delay = (int)pingTicks.Diff();
                    }
                    else if (memory.Span.SequenceEqual(finBytes))
                    {
                        LoggerHelper.Instance.Error($"tunnel connection disponse 5");
                        Dispose();
                    }
                    return;
                }
                await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                    LoggerHelper.Instance.Error(Encoding.UTF8.GetString(memory.Span));
                    LoggerHelper.Instance.Error(string.Join(",", memory.ToArray()));
                }

            }
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        LoggerHelper.Instance.Error($"tunnel connection disponse 4");
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
            byte[] heartData = ArrayPool<byte>.Shared.Rent(4 + data.Length);

            data.Length.ToBytes(heartData.AsMemory());
            data.AsMemory().CopyTo(heartData.AsMemory(4));

            await SendAsync(heartData.AsMemory(0, 4 + data.Length));

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private async Task Sender()
        {
            pipeSender = new Pipe(new PipeOptions(pauseWriterThreshold: maxRemaining, resumeWriterThreshold: (maxRemaining / 2), useSynchronizationContext: false));
            byte[] encodeBuffer = ArrayPool<byte>.Shared.Rent(65 * 1024);
            encodeBuffer[0] = 2; //relay
            encodeBuffer[1] = 1; //forward
            int index = Type == TunnelType.Relay ? 0 : 2;
            int lengthPlus = Type == TunnelType.Relay ? 2 : 0;
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    ReadResult result = await pipeSender.Reader.ReadAsync(cts.Token).ConfigureAwait(false);
                    if (result.IsCompleted && result.Buffer.IsEmpty)
                    {
                        cts.Cancel();
                        break;
                    }

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SendBytes += buffer.Length;
                    long offset = 0;

                    do
                    {
                        //读取包长度
                        int packetLength = 0;
                        if (buffer.First.Length >= 4)
                        {
                            packetLength = buffer.First.ToInt32();
                        }
                        else
                        {
                            //长度标识跨段了
                            buffer.Slice(0, 4).CopyTo(encodeBuffer.AsSpan(2));
                            packetLength = encodeBuffer.AsSpan(2).ToInt32();
                        }
                        //数据量不够
                        if (packetLength + 4 > buffer.Length) break;

                        //复制一份
                        ReadOnlySequence<byte> temp = buffer.Slice(4, packetLength);
                        temp.CopyTo(encodeBuffer.AsSpan(2));

                        int sendLength = packetLength + lengthPlus;
                        if (SSL)
                        {
                            var data = Crypto.Encode(encodeBuffer, 2, packetLength);
                            data.AsMemory().CopyTo(encodeBuffer.AsMemory(2));
                            sendLength = data.Length + lengthPlus;
                        }

                        await UdpClient.SendToAsync(encodeBuffer.AsMemory(index, sendLength), IPEndPoint, cts.Token).ConfigureAwait(false);
                        SendBytes += packetLength;

                        Interlocked.Add(ref sendRemaining, -packetLength);

                        //移动位置
                        offset += 4 + packetLength;
                        //去掉已处理部分
                        buffer = buffer.Slice(4 + packetLength);

                    } while (buffer.Length > 4);

                    //告诉管道已经处理了多少数据，检查了多少数据
                    pipeSender.Reader.AdvanceTo(result.Buffer.GetPosition(offset), result.Buffer.End);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                Dispose();
            }
            ArrayPool<byte>.Shared.Return(encodeBuffer);
        }

        private readonly SemaphoreSlim slm = new SemaphoreSlim(1);
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await slm.WaitAsync(cts.Token);
            try
            {
                await pipeSender.Writer.WriteAsync(data);
                Interlocked.Add(ref sendRemaining, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                LoggerHelper.Instance.Error($"tunnel connection disponse 2");
                Dispose();
            }
            finally
            {
                slm.Release();
            }
            return false;
        }
        public async Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return await SendAsync(buffer.AsMemory(offset, length)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (udpClient == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            SendPingPong(finBytes).ContinueWith((result) =>
            {

                LastTicks.Clear();
                if (Receive == true)
                    UdpClient?.SafeClose();
                udpClient = null;

                cts?.Cancel();
                callback?.Closed(this, userToken);
                callback = null;
                userToken = null;

                Crypto?.Dispose();

                pipeSender?.Writer.Complete();
                pipeSender?.Reader.Complete();

                GC.Collect();
            });
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