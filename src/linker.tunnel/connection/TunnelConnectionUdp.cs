using linker.fec;
using linker.kcp;
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

        public string RemoteMachineId { get; set; }
        public string RemoteMachineName { get; set; }
        public string TransactionId { get; set; }
        public Dictionary<string, string> Configure { get; init; }
        public string TransportName { get; set; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; set; }
        public TunnelType Type { get; set; }
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
        delegate ValueTask RecvFunc(ReadOnlyMemory<byte> data);
        private RecvFunc recvFunc = null;

        const byte PacketTypeData = 0;
        const byte PacketTypePing = 1;
        const byte PacketTypePong = 2;
        const byte PacketTypeFin = 4;

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

            if (TransactionId != "tuntap")
            {
                InitializeKcp();
            }
            else
            {
                InitializeFec();
            }

            if (Receive)
            {
                _ = ProcessWrite();
            }
            _ = ProcessHeart();
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
        public async ValueTask<bool> ProcessWrite(byte[] buffer, int offset, int length)
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
                        Dispose();
                        break;
                    }

                    if (LastTicks.DiffGreater(3000))
                    {
                        pingTicks.Update();
                        await SendPingPong(pingBytes, PacketTypePing).ConfigureAwait(false);
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
        private async ValueTask SendPingPong(byte[] data, byte value)
        {
            byte[] heartData = ArrayPool<byte>.Shared.Rent(4 + data.Length);

            ((ushort)data.Length).ToBytes(heartData.AsMemory());
            data.AsMemory().CopyTo(heartData.AsMemory(4));
            heartData[2] = value;

            await SendAsync(heartData.AsMemory(0, 4 + data.Length)).ConfigureAwait(false);

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private readonly SemaphoreSlim slm = new SemaphoreSlim(1);
        public async ValueTask<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (cts.IsCancellationRequested) return false;

            await slm.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                ((ushort)(data.ToUInt16() + 2)).ToBytes(data);
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
        public ValueTask<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            return SendAsync(buffer.AsMemory(offset, length));
        }

        private ValueTask<int> SendHook(ReadOnlyMemory<byte> data)
        {
            if (SSL)
            {
                Crypto.TryEncode(data.Span.Slice(2), cryptoEncodeBuffer.AsSpan(2), out int bytesWritten);
                ((ushort)bytesWritten).ToBytes(cryptoEncodeBuffer.AsSpan());

                SendBytes += bytesWritten;

                var memory = cryptoEncodeBuffer.AsMemory(0, bytesWritten + 2);
                return sendFunc(memory);
            }
            else
            {
                SendBytes += data.Length;
                return sendFunc(data);
            }
        }
        private ValueTask RecvHook(ReadOnlyMemory<byte> data)
        {
            ReceiveBytes += data.Length;
            LastTicks.Update();
            return recvFunc(data);

        }
        private ValueTask ProcessPacket(ReadOnlyMemory<byte> memory)
        {
            try
            {
                return memory.Span[0] switch
                {
                    PacketTypeData => callback.Receive(this, memory.Slice(2), this.userToken),
                    PacketTypePing => SendPingPong(pongBytes, PacketTypePong),
                    PacketTypePong => ProcessPong(),
                    PacketTypeFin => ProcessFin(),
                    _ => ValueTask.CompletedTask
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return ValueTask.CompletedTask;
        }
        private ValueTask ProcessPong()
        {
            Delay = (int)pingTicks.Diff();
            return ValueTask.CompletedTask;
        }
        private ValueTask ProcessFin()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private ValueTask<int> SendAsyncDefault(ReadOnlyMemory<byte> data)
        {
            return UdpClient.SendToAsync(data.Slice(2), IPEndPoint, cts.Token);
        }
        private ValueTask RecvAsyncDefault(ReadOnlyMemory<byte> data)
        {
            if (SSL)
            {
                Crypto.TryDecode(data.Span, cryptoDecodeBuffer, out int bytesWritten);
                data = cryptoDecodeBuffer.AsMemory(0, bytesWritten);
            }

            return ProcessPacket(data);
        }


        private LinkerFecCodec fecEncoder;
        private LinkerFecCodec fecDecoder;
        private byte[] fecEncodeBuffer;
        private byte[] fecDecodeBuffer;
        private StickyPacketCodec stickyEncoder;
        private LinkerFecOptions fecOption;
        private void InitializeFec()
        {
            try
            {
                LinkerFecRepairProfilePoint[] profile = [];
                if (Configure.TryGetValue("fec", out string fec) && string.IsNullOrWhiteSpace(fec) == false)
                {
                    try
                    {
                        profile = fec.DeJson<LinkerFecRepairProfilePoint[]>();
                    }
                    catch (Exception)
                    {
                    }
                }
                if (profile.Count(c => c.SourceSymbols != 0 && c.RepairSymbols != 0) == 0)
                {
                    return;
                }

                fecOption = new LinkerFecOptions
                {
                    SourceSymbolsPerBlock = 10,
                    RepairSymbolsPerBlock = 4,
                    SymbolSize = 1420 + LinkerFecEncodedSymbol.HeaderSize,
                    RepairProfile = profile,
                };
                fecEncoder = new LinkerFecCodec(fecOption);
                fecDecoder = new LinkerFecCodec(fecOption);
                fecEncodeBuffer = new byte[fecOption.MaxEncodeBufferSize];
                fecDecodeBuffer = new byte[fecOption.MaxDecodeBufferSize];

                stickyEncoder = new StickyPacketCodec(maxRemaining, fecOption.MaxEncodeBufferSize, fecOption.MaxSourceSymbolsPerEncodedBlock);

                sendFunc = SendAsyncFec;
                recvFunc = RecvAsyncFec;
                _ = FlushTaskFec();
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private async ValueTask<int> SendAsyncFec(ReadOnlyMemory<byte> data)
        {
            await stickyEncoder.WriteAsync(data, cts.Token).ConfigureAwait(false);
            return data.Length;
        }
        private async ValueTask RecvAsyncFec(ReadOnlyMemory<byte> data)
        {
            if (fecDecoder.TryDecodeFrame(data, fecDecodeBuffer, out int bytesWritten, out int packetCount))
            {
                Memory<byte> packets = fecDecodeBuffer.AsMemory(0, bytesWritten);
                for (int i = 0; i < packetCount; i++)
                {
                    int packetLength = BinaryPrimitives.ReadUInt16LittleEndian(packets.Span);
                    Memory<byte> packet = packets.Slice(LinkerFecOptions.RecordLengthPrefixSize, packetLength);

                    if (SSL)
                    {
                        Crypto.TryDecode(packet.Span, cryptoDecodeBuffer, out int _bytesWritten);
                        packet = cryptoDecodeBuffer.AsMemory(0, _bytesWritten);
                    }
                    await ProcessPacket(packet).ConfigureAwait(false);

                    packets = packets.Slice(LinkerFecOptions.RecordLengthPrefixSize + packetLength);
                }
            }
        }
        private async Task FlushTaskFec()
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
                        int packetLength = BinaryPrimitives.ReadUInt16LittleEndian(memory.Span);
                        Memory<byte> packet = memory.Slice(LinkerFecOptions.FrameLengthPrefixSize, packetLength);
                        try
                        {
                            await UdpClient.SendToAsync(packet, IPEndPoint, cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (cts.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (SocketException)
                        {
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        memory = memory.Slice(LinkerFecOptions.FrameLengthPrefixSize + packetLength);
                    }
                }
            }
        }


        private KcpConnection kcpConnection;
        private void InitializeKcp()
        {
            kcpConnection = new KcpConnection(12138, 1500, 8192, 1, 10, 2, 1, UdpClient, IPEndPoint, false);
            sendFunc = SendAsyncKcp;
            recvFunc = RecvAsyncKcp;
            _ = FlushTaskKcp();

        }
        private async ValueTask<int> SendAsyncKcp(ReadOnlyMemory<byte> data)
        {
            await kcpConnection.SendAsync(data.Slice(2), cts.Token).ConfigureAwait(false);
            return data.Length;
        }
        private ValueTask RecvAsyncKcp(ReadOnlyMemory<byte> data)
        {
            kcpConnection.Input(data);
            return ValueTask.CompletedTask;
        }
        private async Task FlushTaskKcp()
        {
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(64 * 1024);

            while (!cts.IsCancellationRequested)
            {
                int length = await kcpConnection.ReceiveAsync(owner.Memory, cts.Token).ConfigureAwait(false);
                if (length <= 0)
                {
                    return;
                }

                Memory<byte> packets = owner.Memory.Slice(0, length);

                do
                {
                    int packetLength = packets.ToUInt16();
                    Memory<byte> packet = packets.Slice(2, packetLength);
                    if (SSL)
                    {
                        Crypto.TryDecode(packet.Span, cryptoDecodeBuffer, out int _bytesWritten);
                        packet = cryptoDecodeBuffer.AsMemory(0, _bytesWritten);
                    }
                    await ProcessPacket(packet).ConfigureAwait(false);

                    packets = packets.Slice(2 + packetLength);

                } while (packets.Length > 0);
            }
        }

        public void Dispose()
        {
            if (callback == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            var _callback = callback;
            callback = null;

            SendPingPong(finBytes, PacketTypeFin).AsTask().ContinueWith((result) =>
            {

                LastTicks.Clear();
                if (Receive == true)
                    UdpClient?.SafeClose();

                cts?.Cancel();
                Crypto?.Dispose();
                fecEncoder?.Dispose();
                fecDecoder?.Dispose();
                stickyEncoder?.Dispose();

                kcpConnection?.DisposeAsync();

                GC.Collect();

                _callback?.Closed(this, userToken);
                userToken = null;
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