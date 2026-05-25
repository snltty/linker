using linker.fec;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

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
        public Dictionary<string, string> Configure { get; init; }
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

        public bool Connected => UdpClient != null && LastTicks.Expired(60000) == false;
        public int Delay { get; private set; }

        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();

        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }

        private long sendRemaining = 0;
        public long SendBufferRemaining { get => sendRemaining; }
        public long SendBufferFree { get => maxRemaining - sendRemaining; }
        private const long maxRemaining = 128 * 1024;

        public long RecvBufferRemaining { get; }
        public long RecvBufferFree { get => maxRemaining; }

        public bool Receive { get; init; } = true;
        public bool Send { get; set; } = true;

        [JsonIgnore]
        public Socket UdpClient { get; init; }
        [JsonIgnore]
        public ICrypto Crypto { get; init; }
        private readonly byte[] cryptoEncodeBuffer = new byte[8 * 1024];
        private readonly byte[] cryptoDecodeBuffer = new byte[8 * 1024];

        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cts;
        private object userToken;

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");
        private readonly byte[] finBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.fing");

        delegate ValueTask<int> SendFunc(ReadOnlyMemory<byte> data);
        private SendFunc sendFunc = null;
        delegate Task RecvFunc(ReadOnlyMemory<byte> data);
        private RecvFunc recvFunc = null;

        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null)
            {
                return;
            }

            this.callback = callback;
            this.userToken = userToken;
            this.cts = new CancellationTokenSource();
            this.sendFunc = SendAsyncDefault;
            this.recvFunc = RecvAsyncDefault;

            //InitializeFec();

            if (Receive)
            {
                _ = ProcessWrite();
            }
            if (Send)
            {
                _ = ProcessHeart();
            }
        }
        private async Task ProcessWrite()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            IPEndPoint ep = new IPEndPoint(UdpClient.DualMode || IPEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
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
                    await RecvHook(buffer.AsMemory(0, result.ReceivedBytes)).ConfigureAwait(false);
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
                LoggerHelper.Instance.Error($"tunnel connection dispose 6");
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
            await RecvHook(buffer.AsMemory(offset, length)).ConfigureAwait(false);

            return true;
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cts.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        LoggerHelper.Instance.Error($"tunnel connection dispose 4");
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

            await SendAsync(heartData.AsMemory(0, 4 + data.Length)).ConfigureAwait(false);

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private readonly SemaphoreSlim slm = new SemaphoreSlim(1);
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                await SendHook(data).ConfigureAwait(false);
                return true;
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
                slm.Release();
            }
            return false;
        }
        public Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return SendAsync(buffer.AsMemory(offset, length));
        }

        private ValueTask<int> SendHook(ReadOnlyMemory<byte> data)
        {
            if (SSL)
            {
                Crypto.TryEncode(data.Span.Slice(4), cryptoEncodeBuffer.AsSpan(4), out int bytesWritten);
                bytesWritten.ToBytes(cryptoEncodeBuffer.AsMemory());

                SendBytes += bytesWritten;

                return sendFunc(cryptoEncodeBuffer.AsMemory(0, bytesWritten + 4));
            }
            else
            {
                SendBytes += data.Length;
                return sendFunc(data);
            }
        }
        private async Task RecvHook(ReadOnlyMemory<byte> data)
        {
            if (Send)
            {
                await recvFunc(data).ConfigureAwait(false);
            }
            else
            {
                await callback.Receive(this, data, this.userToken).ConfigureAwait(false);
            }
            ReceiveBytes += data.Length;
            LastTicks.Update();
        }
        private async Task CallbackPacket(ReadOnlyMemory<byte> memory)
        {
            try
            {
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
                        LoggerHelper.Instance.Error($"tunnel connection dispose 5");
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

        private ValueTask<int> SendAsyncDefault(ReadOnlyMemory<byte> data)
        {
            return UdpClient.SendToAsync(data.Slice(4), IPEndPoint, cts.Token);
        }
        private Task RecvAsyncDefault(ReadOnlyMemory<byte> data)
        {
            if (SSL)
            {
                Crypto.TryDecode(data.Span, cryptoDecodeBuffer, out int bytesWritten);
                data = cryptoDecodeBuffer.AsMemory(0, bytesWritten);
            }
            return CallbackPacket(data);
        }


        private LinkerFecCodec fecEncoder;
        private LinkerFecCodec fecDecoder;
        private byte[] fecEncodeBuffer;
        private byte[] fecDecodeBuffer;
        private StickyPacketCodec stickyEncoder;
        private LinkerFecOptions fecOption;
        private void InitializeFec()
        {
            fecOption = new LinkerFecOptions
            {
                SourceSymbolsPerBlock = 10,
                RepairSymbolsPerBlock = 4,
                SymbolSize = 1420 + LinkerFecEncodedSymbol.HeaderSize,
                RepairProfile = [
                   new LinkerFecRepairProfilePoint(1, 2),
                    new LinkerFecRepairProfilePoint(10, 4)
                ],
            };
            fecEncoder = new LinkerFecCodec(fecOption);
            fecDecoder = new LinkerFecCodec(fecOption);
            fecEncodeBuffer = new byte[fecOption.MaxEncodeBufferSize];
            fecDecodeBuffer = new byte[fecOption.MaxDecodeBufferSize];

            stickyEncoder = new StickyPacketCodec(maxRemaining, fecOption.MaxEncodeBufferSize, fecOption.MaxSourceSymbolsPerEncodedBlock);

            sendFunc = SendAsyncFec;
            recvFunc = RecvAsyncFec;
            FlushTask();
        }
        private async ValueTask<int> SendAsyncFec(ReadOnlyMemory<byte> data)
        {
            await stickyEncoder.WriteAsync(data, cts.Token).ConfigureAwait(false);
            return data.Length;
        }
        private async Task RecvAsyncFec(ReadOnlyMemory<byte> data)
        {
            if (fecDecoder.TryDecodeFrame(data, fecDecodeBuffer, out int bytesWritten, out int packetCount))
            {
                Memory<byte> packets = fecDecodeBuffer.AsMemory(0, bytesWritten);
                for (int i = 0; i < packetCount; i++)
                {
                    int packetLength = BinaryPrimitives.ReadInt32LittleEndian(packets.Span);
                    Memory<byte> packet = packets.Slice(sizeof(int), packetLength);

                    if (SSL)
                    {
                        Crypto.TryDecode(packet.Span, cryptoDecodeBuffer, out int _bytesWritten);
                        packet = cryptoDecodeBuffer.AsMemory(0, _bytesWritten);
                    }
                    await CallbackPacket(packet).ConfigureAwait(false);

                    packets = packets.Slice(sizeof(int) + packetLength);
                }
            }
        }
        private void FlushTask()
        {
            _ = FlushCallbackOther();
        }
        private async Task FlushCallbackOther()
        {
            while (cts.IsCancellationRequested == false)
            {
                ReadOnlyMemory<byte> packets = await stickyEncoder.ReadPacketsAsync(cts.Token).ConfigureAwait(false);
                if (packets.Length == 0)
                {
                    if (stickyEncoder.IsCompleted)
                    {
                        cts.Cancel();
                        break;
                    }
                    continue;
                }
                if (fecEncoder.TryEncodePacket(packets, fecEncodeBuffer, out int bytesWritten, out int packetCount))
                {
                    var memory = fecEncodeBuffer.AsMemory(0, bytesWritten);

                    for (int i = 0; i < packetCount; i++)
                    {
                        int packetLength = BinaryPrimitives.ReadInt32LittleEndian(memory.Span);
                        Memory<byte> packet = memory.Slice(sizeof(int), packetLength);
                        try
                        {
                            await UdpClient.SendToAsync(packet, IPEndPoint, cts.Token).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                        }
                        memory = memory.Slice(sizeof(int) + packetLength);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (callback == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            SendPingPong(finBytes).ContinueWith((result) =>
            {

                LastTicks.Clear();
                if (Receive == true)
                    UdpClient?.SafeClose();

                cts?.Cancel();
                callback?.Closed(this, userToken);
                callback = null;
                userToken = null;

                Crypto?.Dispose();

                fecEncoder?.Dispose();
                fecDecoder?.Dispose();
                stickyEncoder?.Dispose();

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