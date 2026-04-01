using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace linker.nat
{
    public sealed class LinkerSrcProxy
    {
        public bool Running => listenSocketTcp != null;
        private Socket listenSocketTcp;

        ushort proxyPort = 0;
        uint tunIp = 0;
        uint proxySrc = 0;

        private ILinkerSrcProxyCallback callback;

        private readonly ConcurrentDictionary<SrcKey, SrcCacheInfo> srcMap = new(new SrcKeyComparer());
        private readonly ConcurrentDictionary<NatKey, ConnectionState> connections = new(new NatKeyComparer());

        public LinkerSrcProxy()
        {
            SrcMapClearTask();
        }
        public bool Setup(IPAddress srcAddr, byte prefixLength, ILinkerSrcProxyCallback callback, ref string error)
        {
            this.callback = callback;
            Shutdown();
            try
            {
                listenSocketTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocketTcp.Bind(new IPEndPoint(IPAddress.Any, 0));
                listenSocketTcp.Listen(int.MaxValue);

                proxySrc = NetworkHelper.ToNetworkValue(srcAddr, prefixLength);
                tunIp = NetworkHelper.ToValue(srcAddr);
                proxyPort = (ushort)(listenSocketTcp.LocalEndPoint as IPEndPoint).Port;

                _ = AcceptAsync().ConfigureAwait(false);

                error = string.Empty;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            return true;
        }
        private async Task AcceptAsync()
        {
            int hashcode = listenSocketTcp.GetHashCode();
            try
            {
                while (true)
                {
                    Socket source = await listenSocketTcp.AcceptAsync().ConfigureAwait(false);
                    IPEndPoint local = source.LocalEndPoint as IPEndPoint;
                    IPEndPoint remote = source.RemoteEndPoint as IPEndPoint;

                    SrcKey key = new SrcKey(NetworkHelper.ToValue(local.Address), (ushort)remote.Port);
                    if (srcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        source.SafeClose();
                        continue;
                    }
                    _ = ConnectAsync(source, cache).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                if (listenSocketTcp != null && listenSocketTcp.GetHashCode() == hashcode)
                    Shutdown();
            }
        }

        private async Task ConnectAsync(Socket source, SrcCacheInfo cache)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024 + 40 + 4);
            NatKey key = new NatKey(tunIp, cache.SrcPort, cache.DstAddr, cache.DstPort);
            try
            {
                ConnectionState state = new ConnectionState
                {
                    Socket = source,
                    ReadPacket = new LinkerSrcProxyReadPacket(buffer),
                    Tcs = new TaskCompletionSource()
                };
                state.ReadPacket.IPHeadLength = 20;
                state.ReadPacket.Version = 4;
                state.ReadPacket.Dscp = 0;
                state.ReadPacket.Identification = 0;
                state.ReadPacket.IpFlags = 0;
                state.ReadPacket.FragmentOffset = 0;
                state.ReadPacket.Ttl = 64;
                state.ReadPacket.ProtocolType = ProtocolType.Tcp;
                state.ReadPacket.SrcAddr = tunIp;
                state.ReadPacket.DstAddr = cache.DstAddr;
                state.ReadPacket.IPChecksum = 1;

                state.ReadPacket.SrcPort = cache.SrcPort;
                state.ReadPacket.DstPort = cache.DstPort;
                state.ReadPacket.Seq = 0;
                state.ReadPacket.Cq = 0;
                state.ReadPacket.HeadLength = 20;
                state.ReadPacket.Reserved = 0;
                state.ReadPacket.Flags = LinkerSrcProxyFlags.Syn;
                state.ReadPacket.WindowSize = 65535;
                state.ReadPacket.PayloadChecksum = 1;
                connections.TryAdd(key, state);

                state.ReadPacket.Flags = LinkerSrcProxyFlags.Syn;
                state.ReadPacket.TotalLength = 40;
                state.ReadPacket.Length = 44;
                await callback.Callback(state.ReadPacket).ConfigureAwait(false);

                await state.Tcs.WithTimeout(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);

                state.ReadPacket.Flags = LinkerSrcProxyFlags.Psh;
                await Task.WhenAny(Rcver(state, buffer), Sender(state)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                if (connections.TryRemove(key, out ConnectionState state))
                {
                    state.ReadPacket.Flags = LinkerSrcProxyFlags.Rst;
                    state.ReadPacket.TotalLength = 40;
                    state.ReadPacket.Length = 44;
                    await callback.Callback(state.ReadPacket).ConfigureAwait(false);
                    state.Dispose();
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public void Shutdown()
        {
            listenSocketTcp?.SafeClose();
            listenSocketTcp = null;

            foreach (var item in connections.Values)
            {
                item.Dispose();
            }
            srcMap.Clear();
            connections.Clear();
        }


        public async ValueTask<bool> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp)
        {
            (NatKey key, LinkerSrcProxyFlags flag, ushort win, bool next) = GetKeyAndFlag(packet);
            if (next == false) return true;

            switch (flag)
            {
                case LinkerSrcProxyFlags.Psh:
                    await HandlePsh(packet, key, win).ConfigureAwait(false);
                    break;
                case LinkerSrcProxyFlags.Syn:
                    _ = HandleSyn(key, originDstIp).ConfigureAwait(false);
                    break;
                case LinkerSrcProxyFlags.SynAck:
                    HandleSynAck(key);
                    break;
                case LinkerSrcProxyFlags.Rst:
                    HandleRst(key);
                    break;
                default:
                    break;
            }
            return false;
        }
        private unsafe (NatKey key, LinkerSrcProxyFlags flag, ushort win, bool next) GetKeyAndFlag(ReadOnlyMemory<byte> packet)
        {
            fixed (byte* ptr = packet.Span)
            {
                LinkerSrcProxyWritePacket writePacket = new LinkerSrcProxyWritePacket(ptr);
                return (new NatKey(writePacket.SrcAddr, writePacket.SrcPort, writePacket.DstAddr, writePacket.DstPort),
                    writePacket.Flag, writePacket.WindowSize, writePacket.Seq == 0 && writePacket.Cq == 0);
            }
        }

        private async Task HandleSyn(NatKey key, uint originDstIp)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024 + 40 + 4);
            try
            {
                Socket source = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await source.ConnectAsync(new IPEndPoint(NetworkHelper.ToIP(key.DstIp), key.DstPort), cts.Token).ConfigureAwait(false);

                ConnectionState state = new ConnectionState
                {
                    Socket = source,
                    ReadPacket = new LinkerSrcProxyReadPacket(buffer)
                };
                state.ReadPacket.IPHeadLength = 20;
                state.ReadPacket.Version = 4;
                state.ReadPacket.Dscp = 0;
                state.ReadPacket.Identification = 0;
                state.ReadPacket.IpFlags = 0;
                state.ReadPacket.FragmentOffset = 0;
                state.ReadPacket.Ttl = 64;
                state.ReadPacket.ProtocolType = ProtocolType.Tcp;
                state.ReadPacket.SrcAddr = originDstIp;
                state.ReadPacket.DstAddr = key.SrcIp;
                state.ReadPacket.IPChecksum = 1;

                state.ReadPacket.SrcPort = key.DstPort;
                state.ReadPacket.DstPort = key.SrcPort;
                state.ReadPacket.Seq = 0;
                state.ReadPacket.Cq = 0;
                state.ReadPacket.HeadLength = 20;
                state.ReadPacket.Reserved = 0;
                state.ReadPacket.Flags = LinkerSrcProxyFlags.Syn;
                state.ReadPacket.WindowSize = 65535;
                state.ReadPacket.PayloadChecksum = 1;
                connections.AddOrUpdate(key, state, (a, b) => state);

                state.ReadPacket.TotalLength = 40;
                state.ReadPacket.Length = 44;
                state.ReadPacket.Flags = LinkerSrcProxyFlags.SynAck;
                await callback.Callback(state.ReadPacket).ConfigureAwait(false);

                state.ReadPacket.Flags = LinkerSrcProxyFlags.Psh;
                await Task.WhenAny(Rcver(state, buffer), Sender(state)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                if (connections.TryRemove(key, out ConnectionState connection))
                {
                    connection.ReadPacket.Flags = LinkerSrcProxyFlags.Rst;
                    connection.ReadPacket.TotalLength = 40;
                    connection.ReadPacket.Length = 44;
                    await callback.Callback(connection.ReadPacket).ConfigureAwait(false);
                    connection.Dispose();
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private void HandleSynAck(NatKey key)
        {
            if (connections.TryGetValue(key, out ConnectionState state))
            {
                state.Tcs?.SetResult();
            }
        }
        private async ValueTask HandlePsh(ReadOnlyMemory<byte> packet, NatKey key, ushort window)
        {
            if (connections.TryGetValue(key, out ConnectionState state))
            {
                try
                {
                    state.Sending = window > 0;
                    ReadOnlyMemory<byte> memory = packet.Slice(40);
                    await state.Pipe.Writer.WriteAsync(memory).ConfigureAwait(false);
                    state.AddReceived(memory.Length);

                    if (state.NeedPause) await SendWindow(state, 0).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    state.ReadPacket.Flags = LinkerSrcProxyFlags.Rst;
                    state.ReadPacket.TotalLength = 40;
                    state.ReadPacket.Length = 44;
                    await callback.Callback(state.ReadPacket).ConfigureAwait(false);

                    connections.TryRemove(key, out _);
                    state.Dispose();

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }

        private async Task SendWindow(ConnectionState state, ushort window)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(44);
            try
            {
                state.ReadPacket.WindowSize = window;
                state.Receiving = window > 0;
                state.ReadPacket.Buffer.AsMemory(0, 44).CopyTo(buffer);

                LinkerSrcProxyReadPacket packet = new LinkerSrcProxyReadPacket(buffer);
                packet.TotalLength = 40;
                packet.Length = 44;
                await callback.Callback(packet).ConfigureAwait(false);
                packet.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private async Task Sender(ConnectionState state)
        {
            while (true)
            {
                ReadResult result = await state.Pipe.Reader.ReadAsync().ConfigureAwait(false);
                if (result.IsCompleted && result.Buffer.IsEmpty)
                {
                    break;
                }
                ReadOnlySequence<byte> buffer = result.Buffer;
                foreach (ReadOnlyMemory<byte> memoryBlock in result.Buffer)
                {
                    int length = await state.Socket.SendAsync(memoryBlock, SocketFlags.None).ConfigureAwait(false);
                    state.AddReceived(-memoryBlock.Length);
                    if (state.NeedResume) await SendWindow(state, 1).ConfigureAwait(false);
                }
                state.Pipe.Reader.AdvanceTo(buffer.End);
            }
        }
        private async Task Rcver(ConnectionState state, byte[] buffer)
        {
            int bytesRead;
            while ((bytesRead = await state.Socket.ReceiveAsync(buffer.AsMemory(44), SocketFlags.None).ConfigureAwait(false)) != 0)
            {
                state.ReadPacket.Flags = LinkerSrcProxyFlags.Psh;
                state.ReadPacket.TotalLength = bytesRead + 40;
                state.ReadPacket.Length = bytesRead + 44;
                await callback.Callback(state.ReadPacket).ConfigureAwait(false);

                if (state.Sending == false)
                {
                    while (state.Sending == false && state.Socket != null)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                }
            }
        }

        private void HandleRst(NatKey key)
        {
            if (connections.TryRemove(key, out ConnectionState state))
            {
                state.Dispose();
            }
        }

        public unsafe bool Read(ReadOnlyMemory<byte> packet)
        {
            if (Running == false) return true;

            //《关于TUN虚拟网卡自转发实现TCP/IP三层转四层代理》，目标可以是HTTP/HTTPS/SOCKS5任意代理
            //虚拟网卡IP 10.18.18.2，代理端口33333，假设原始连接 10.18.18.2:11111->10.18.18.3:5201
            fixed (byte* ptr = packet.Span)
            {
                SrcProxyPacket srcProxyPacket = new(ptr);
                if (srcProxyPacket.Protocol != ProtocolType.Tcp || srcProxyPacket.SrcAddr != tunIp) return true;//往下走
                //从代理端口回来的
                if (srcProxyPacket.SrcPort == proxyPort)
                {
                    //(10.18.18.2,22222)、取到说明已建立连接，包括[SYN+ACK/PSH+ACK/ACK/FIN/RST]的任意包
                    if (srcMap.TryGetValue(new SrcKey(srcProxyPacket.SrcAddr, srcProxyPacket.DstPort), out SrcCacheInfo cache))
                    {
                        if (srcProxyPacket.TcpFinOrRst) cache.Fin = true;
                        cache.LastTime = Environment.TickCount64;
                        //3、10.18.18.2:33333->10.18.18.2:22222 改为 10.18.18.3:5201->10.18.18.2:11111 
                        srcProxyPacket.DstAddr = srcProxyPacket.SrcAddr;
                        srcProxyPacket.DstPort = cache.SrcPort;
                        srcProxyPacket.SrcAddr = cache.DstAddr;
                        srcProxyPacket.SrcPort = cache.DstPort;
                        srcProxyPacket.IPChecksum = 0; //需要重新计算IP头校验和
                        srcProxyPacket.PayloadChecksum = 0; //需要重新计算TCP校验和
                    }
                }
                else //从访问端来的
                {
                    SrcKey key = new SrcKey(srcProxyPacket.SrcAddr, srcProxyPacket.SrcPort);
                    //(10.18.18.2,11111)、取不到是SYN包则建立映射，不是SYN包则继续走原路
                    if (srcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        if (srcProxyPacket.TcpOnlySyn == false) return true; //往下走
                        if (callback.Callback(srcProxyPacket.DstAddr) == false) return true;//不支持代理

                        //1、10.18.18.2:11111->10.18.18.3:5201 [SYN] 新连接
                        cache = new SrcCacheInfo
                        {
                            DstAddr = srcProxyPacket.DstAddr,
                            DstPort = srcProxyPacket.DstPort,
                            SrcPort = srcProxyPacket.SrcPort,
                            NewPort = NetworkHelper.ApplyNewPort() //随机新端口,比如 22222，windows某些版本不需要新端口，可以直接使用11111
                        };
                        //添加 (10.18.18.2,11111)、(10.18.18.2,22222) 作为key的缓存
                        srcMap.AddOrUpdate(new SrcKey(srcProxyPacket.SrcAddr, cache.SrcPort), cache, (a, b) => cache);
                        srcMap.AddOrUpdate(new SrcKey(srcProxyPacket.SrcAddr, cache.NewPort), cache, (a, b) => cache);
                    }
                    if (srcProxyPacket.TcpFinOrRst) cache.Fin = true;
                    cache.LastTime = Environment.TickCount64;
                    //2、10.18.18.2:11111->10.18.18.3:5201 改为 10.18.18.0:22222->10.18.18.2:33333 包括[SYN/PSH+ACK/ACK/FIN/RST]的任意包
                    srcProxyPacket.DstAddr = srcProxyPacket.SrcAddr;
                    srcProxyPacket.DstPort = proxyPort;
                    srcProxyPacket.SrcAddr = proxySrc;
                    srcProxyPacket.SrcPort = cache.NewPort;
                    srcProxyPacket.IPChecksum = 0; //需要重新计算IP头校验和
                    srcProxyPacket.PayloadChecksum = 0;//需要重新计算TCP校验和
                }
                return false;
            }

        }
        private void SrcMapClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                foreach (var item in srcMap.Where(c => c.Value.Fin && Environment.TickCount64 - c.Value.LastTime > 60 * 1000).ToList())
                {
                    srcMap.TryRemove(item.Key, out _);
                }
            }, 30000);
        }




        readonly struct SrcKey
        {
            public readonly uint SrcIp;
            public readonly uint SrcPort;

            public SrcKey(uint srcIp, uint srcPort)
            {
                SrcIp = srcIp;
                SrcPort = srcPort;
            }
        }
        class SrcKeyComparer : IEqualityComparer<SrcKey>
        {
            public bool Equals(SrcKey x, SrcKey y) => x.SrcIp == y.SrcIp &&
                      x.SrcPort == y.SrcPort;

            public int GetHashCode(SrcKey obj)
            {
                return HashCode.Combine(obj.SrcIp, obj.SrcPort);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        readonly struct NatKey
        {
            public readonly uint SrcIp;
            public readonly uint DstIp;
            public readonly ushort SrcPort;
            public readonly ushort DstPort;

            public NatKey(uint srcIp, ushort srcPort, uint dstIp, ushort dstPort)
            {
                SrcIp = srcIp;
                SrcPort = srcPort;
                DstIp = dstIp;
                DstPort = dstPort;
            }
        }
        class NatKeyComparer : IEqualityComparer<NatKey>
        {
            public bool Equals(NatKey x, NatKey y) => (x.SrcIp == y.SrcIp && x.SrcPort == y.SrcPort &&
                    x.DstIp == y.DstIp && x.DstPort == y.DstPort) || (x.SrcIp == y.DstIp && x.SrcPort == y.DstPort &&
                       x.DstIp == y.SrcIp && x.DstPort == y.SrcPort);

            public int GetHashCode(NatKey obj)
            {
                return HashCode.Combine(obj.SrcIp, obj.SrcPort, obj.DstIp, obj.DstPort);
            }
        }


        sealed class ConnectionState
        {
            public Socket Socket { get; set; }
            public LinkerSrcProxyReadPacket ReadPacket { get; init; }
            public TaskCompletionSource Tcs { get; init; }
            public Pipe Pipe { get; init; } = new Pipe(new PipeOptions(minimumSegmentSize: 8192, pauseWriterThreshold: 2 * 1024 * 1024, resumeWriterThreshold: 512 * 1024, useSynchronizationContext: false));
            private long received = 0;
            public long Received => received;

            public bool Sending { get; set; } = true;
            public bool Receiving { get; set; } = true;

            public void AddReceived(long value)
            {
                Interlocked.Add(ref received, value);
            }
            public bool NeedPause => Received > 512 * 1024 && Receiving;
            public bool NeedResume => Received < 128 * 1024 && Receiving == false;

            public void Dispose()
            {
                Pipe?.Writer.Complete();
                Pipe?.Reader.Complete();
                Socket?.SafeClose();
                Socket = null;
                ReadPacket?.Dispose();
            }
        }
        readonly unsafe struct LinkerSrcProxyWritePacket
        {
            private readonly byte* ptr;
            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly ProtocolType Protocol => (ProtocolType)(*(ptr + 9));
            public readonly int IPHeadLength => (*ptr & 0b1111) * 4;
            public readonly byte* PayloadPtr => ptr + IPHeadLength;

            public readonly uint SrcAddr => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
            public readonly ushort SrcPort => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr));
            public readonly uint DstAddr => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
            public readonly ushort DstPort => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 2));

            public readonly uint Seq => BinaryPrimitives.ReverseEndianness(*(uint*)(PayloadPtr + 4));
            public readonly uint Cq => BinaryPrimitives.ReverseEndianness(*(uint*)(PayloadPtr + 8));


            public byte DataOffset => (byte)((*(PayloadPtr + 12) >> 4) & 0b1111);
            public int HeadLength => DataOffset * 4;
            public readonly LinkerSrcProxyFlags Flag => (LinkerSrcProxyFlags)(*(PayloadPtr + 13));

            public ushort WindowSize => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 14));

            public unsafe LinkerSrcProxyWritePacket(byte* ptr)
            {
                this.ptr = ptr;
            }
        }

        sealed class SrcCacheInfo
        {
            public long LastTime { get; set; } = Environment.TickCount64;
            public uint DstAddr { get; set; }
            public ushort DstPort { get; set; }
            public ushort SrcPort { get; set; }
            public ushort NewPort { get; set; }
            public bool Fin { get; set; }
        }
        readonly unsafe struct SrcProxyPacket
        {
            private readonly byte* ptr;

            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly ProtocolType Protocol => (ProtocolType)(*(ptr + 9));
            public readonly int IPHeadLength => (*ptr & 0b1111) * 4;
            public readonly byte* PayloadPtr => ptr + IPHeadLength;
            public readonly uint SrcAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
                }
                set
                {
                    *(uint*)(ptr + 12) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public readonly uint DstAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
                }
                set
                {
                    *(uint*)(ptr + 16) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public readonly ushort SrcPort
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr));
                }
                set
                {
                    *(ushort*)(PayloadPtr) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public readonly ushort DstPort
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 2));
                }
                set
                {
                    *(ushort*)(PayloadPtr + 2) = BinaryPrimitives.ReverseEndianness(value);
                }
            }

            public byte DataOffset
            {
                get
                {
                    return (byte)((*(PayloadPtr + 12) >> 4) & 0b1111);
                }
                set
                {
                    *(PayloadPtr + 12) = (byte)((*(PayloadPtr + 12) & 0b00001111) | ((value & 0b1111) << 4));
                }
            }


            const byte fin = 1;
            const byte syn = 2;
            const byte rst = 4;
            const byte psh = 8;
            const byte ack = 16;
            const byte urg = 32;
            public readonly byte TcpFlag
            {
                get
                {
                    return *(PayloadPtr + 13);
                }
                set
                {
                    *(PayloadPtr + 13) = value;
                }
            }
            public readonly bool TcpFlagFin => (TcpFlag & fin) != 0;
            public readonly bool TcpFlagSyn => (TcpFlag & syn) != 0;
            public readonly bool TcpFlagRst => (TcpFlag & rst) != 0;
            public readonly bool TcpFlagPsh => (TcpFlag & psh) != 0;
            public readonly bool TcpFlagAck => (TcpFlag & ack) != 0;
            public readonly bool TcpFlagUrg => (TcpFlag & urg) != 0;

            public readonly bool TcpPshAck => (TcpFlag & (psh | ack)) == (psh | ack);
            public readonly bool TcpOnlyAck => TcpFlag == ack;
            public readonly bool TcpOnlySyn => TcpFlag == syn;
            public readonly bool TcpSynAck => TcpFlag == (syn | ack);
            public readonly bool TcpFinOrRst => (TcpFlag & (fin | rst)) != 0;


            public readonly ushort IPChecksum
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 10));
                }
                set
                {
                    *(ushort*)(ptr + 10) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            public readonly ushort PayloadChecksum
            {
                get
                {
                    return Protocol switch
                    {
                        ProtocolType.Icmp => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 2)),
                        ProtocolType.Tcp => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 16)),
                        ProtocolType.Udp => BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 6)),
                        _ => (ushort)0,
                    };
                }
                set
                {
                    switch (Protocol)
                    {
                        case ProtocolType.Icmp:
                            *(ushort*)(PayloadPtr + 2) = BinaryPrimitives.ReverseEndianness(value);
                            break;
                        case ProtocolType.Tcp:
                            *(ushort*)(PayloadPtr + 16) = BinaryPrimitives.ReverseEndianness(value);
                            break;
                        case ProtocolType.Udp:
                            *(ushort*)(PayloadPtr + 6) = BinaryPrimitives.ReverseEndianness(value);
                            break;
                    }
                }
            }

            public SrcProxyPacket(byte* ptr)
            {
                this.ptr = ptr;
            }
        }

    }

    public unsafe sealed class LinkerSrcProxyReadPacket
    {
        private byte* ptr;

        public byte[] Buffer { get; private set; }
        public int Offset => 0;
        public int Length { get; set; }
        public Memory<byte> RawPacket => Buffer.AsMemory(Offset + 4, Length - 4);

        public byte Version
        {
            get
            {
                return (byte)((*ptr >> 4) & 0b1111);
            }
            set
            {
                *ptr = (byte)((*ptr & 0x0F) | ((value & 0x0F) << 4));
            }
        }

        public int IPHeadLength
        {
            get { return (*ptr & 0b1111) * 4; }
            set
            {
                *ptr = (byte)((*ptr & 0b11110000) | ((value / 4) & 0b00001111));
            }
        }
        public byte Dscp
        {
            get
            {
                return *(ptr + 1);
            }
            set
            {
                *(ptr + 1) = value;
            }
        }
        public int TotalLength
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 2));
            }
            set
            {
                value.ToBytes(Buffer.AsMemory());
                *(ushort*)(ptr + 2) = BinaryPrimitives.ReverseEndianness((ushort)value);
            }
        }
        public ushort Identification
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 4));
            }
            set
            {
                *(ushort*)(ptr + 4) = BinaryPrimitives.ReverseEndianness((ushort)value);
            }
        }
        public byte IpFlags
        {
            get
            {
                return (byte)((*(ptr + 6) >> 5) & 0b111);
            }
            set
            {
                *(ptr + 6) = (byte)((*(ptr + 6) & 0b00011111) | ((value & 0b111) << 5));
            }
        }
        public ushort FragmentOffset
        {
            get
            {
                return (ushort)(BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 6)) & 0x1FFF);
            }
            set
            {
                *(ushort*)(ptr + 6) = BinaryPrimitives.ReverseEndianness((ushort)((*(ushort*)(ptr + 6) & 0xE000) | (value & 0x1FFF)));
            }
        }
        public byte Ttl
        {
            get
            {
                return *(ptr + 8);
            }
            set
            {
                *(ptr + 8) = value;
            }
        }
        public ProtocolType ProtocolType
        {
            get
            {
                return (ProtocolType)(*(ptr + 9));
            }
            set
            {
                *(ptr + 9) = (byte)value;
            }
        }
        public ushort IPChecksum
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 10));
            }
            set
            {
                *(ushort*)(ptr + 10) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public uint SrcAddr
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
            }
            set
            {
                *(uint*)(ptr + 12) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public uint DstAddr
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
            }
            set
            {
                *(uint*)(ptr + 16) = BinaryPrimitives.ReverseEndianness(value);
            }
        }

        public byte* PayloadPtr => ptr + IPHeadLength;

        public ushort SrcPort
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr));
            }
            set
            {
                *(ushort*)(PayloadPtr) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public ushort DstPort
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 2));
            }
            set
            {
                *(ushort*)(PayloadPtr + 2) = BinaryPrimitives.ReverseEndianness(value);
            }
        }

        public uint Seq
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(uint*)(PayloadPtr + 4));

            }
            set
            {
                *(uint*)(PayloadPtr + 4) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public uint Cq
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(uint*)(PayloadPtr + 8));
            }
            set
            {
                *(uint*)(PayloadPtr + 8) = BinaryPrimitives.ReverseEndianness(value);
            }
        }

        public byte DataOffset
        {
            get
            {
                return (byte)((*(PayloadPtr + 12) >> 4) & 0b1111);
            }
            set
            {
                *(PayloadPtr + 12) = (byte)((*(PayloadPtr + 12) & 0b00001111) | ((value & 0b1111) << 4));
            }
        }
        public int HeadLength
        {
            get
            {
                return DataOffset * 4;
            }
            set
            {
                DataOffset = (byte)(value / 4);
            }
        }
        public byte Reserved
        {
            get
            {
                return (byte)((*(PayloadPtr + 12) >> 1) & 0b111);
            }
            set
            {
                *(PayloadPtr + 12) = (byte)((*(PayloadPtr + 12) & 0b11110001) | ((value & 0b111) << 1));
            }
        }
        public LinkerSrcProxyFlags Flags
        {
            get
            {
                return (LinkerSrcProxyFlags)(*(PayloadPtr + 13));
            }
            set
            {
                *(PayloadPtr + 13) = (byte)value;
            }
        }

        public ushort WindowSize
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 14));
            }
            set
            {
                *(ushort*)(PayloadPtr + 14) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public ushort PayloadChecksum
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 16));
            }
            set
            {
                *(ushort*)(PayloadPtr + 16) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public ushort UrgentPointer
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 18));
            }
            set
            {
                *(ushort*)(PayloadPtr + 18) = BinaryPrimitives.ReverseEndianness(value);
            }
        }

        public LinkerSrcProxyReadPacket(byte[] buffer)
        {
            Buffer = buffer;
            handle = buffer.AsMemory().Pin();
            this.ptr = (byte*)handle.Pointer + 4;
        }

        MemoryHandle handle;
        public void Dispose()
        {
            ptr = (byte*)0;
            handle.Dispose();
        }

    }
    public enum LinkerSrcProxyFlags : byte
    {
        Fin = 0b00000001,
        Syn = 0b00000010,
        Rst = 0b00000100,
        Psh = 0b00001000,
        Ack = 0b00010000,
        Urg = 0b00100000,

        SynAck = Syn | Ack,
        UrgAck = Urg | Ack,
    }
    public interface ILinkerSrcProxyCallback
    {
        public Task Callback(LinkerSrcProxyReadPacket packet);
        public bool Callback(uint ip);
    }
}
