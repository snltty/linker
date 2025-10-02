using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using linker.libs.timer;

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

        private readonly ConcurrentDictionary<(uint srcIp, ushort srcPort), SrcCacheInfo> srcMap = new();
        private readonly ConcurrentDictionary<ConnectionKey, ConnectionState> connections = new(new ConnectionKeyComparer());

        public LinkerSrcProxy()
        {
            SrcMapClearTask();
        }
        public bool Setup(IPAddress dstAddr, byte prefixLength, ILinkerSrcProxyCallback callback, ref string error)
        {
            this.callback = callback;
            Shutdown();
            try
            {
                listenSocketTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocketTcp.Bind(new IPEndPoint(IPAddress.Any, 0));
                listenSocketTcp.Listen(int.MaxValue);
                _ = AcceptAsync();

                proxySrc = NetworkHelper.ToNetworkValue(dstAddr, prefixLength);
                tunIp = NetworkHelper.ToValue(dstAddr);
                proxyPort = (ushort)(listenSocketTcp.LocalEndPoint as IPEndPoint).Port;

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
                    Socket source = await listenSocketTcp.AcceptAsync();
                    IPEndPoint local = source.LocalEndPoint as IPEndPoint;
                    IPEndPoint remote = source.RemoteEndPoint as IPEndPoint;

                    (uint srcIp, ushort srcPort) key = (NetworkHelper.ToValue(local.Address), (ushort)remote.Port);
                    if (srcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        source.SafeClose();
                        continue;
                    }
                    _ = Connect(source, cache);
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
        private async Task Connect(Socket source, SrcCacheInfo cache)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024 + 40 + 4);
            ConnectionKey key = new() { srcAddr = tunIp, srcPort = cache.SrcPort, dstAddr = cache.DstAddr, dstPort = cache.DstPort };
            try
            {
                ConnectionState state = new ConnectionState
                {
                    Source = source,
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
                await callback.Callback(state.ReadPacket);

                await state.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);

                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory(44), SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    state.ReadPacket.Flags = LinkerSrcProxyFlags.Psh;
                    state.ReadPacket.TotalLength = bytesRead + 40;
                    state.ReadPacket.Length = bytesRead + 44;
                    await callback.Callback(state.ReadPacket).ConfigureAwait(false);
                }
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
                    connection.Disponse();
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
                item.Disponse();
            }
            srcMap.Clear();
            connections.Clear();
        }

        public async ValueTask<bool> WriteAsync(ReadOnlyMemory<byte> packet)
        {
            using LinkerSrcProxyWritePacket writePacket = new LinkerSrcProxyWritePacket(packet);
            if (writePacket.Seq > 0 || writePacket.Cq > 0)
            {
                return true;
            }

            ConnectionKey key = new() { srcAddr = writePacket.SrcAddr, srcPort = writePacket.SrcPort, dstAddr = writePacket.DstAddr, dstPort = writePacket.DstPort };
            switch (writePacket.Flag)
            {
                case LinkerSrcProxyFlags.Psh:
                    await HandlePsh(packet, key).ConfigureAwait(false);
                    break;
                case LinkerSrcProxyFlags.Syn:
                    _ = HandleSyn(key);
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
        private async ValueTask HandlePsh(ReadOnlyMemory<byte> packet, ConnectionKey key)
        {
            if (connections.TryGetValue(key, out ConnectionState connection))
            {
                try
                {
                    ReadOnlyMemory<byte> memory = packet.Slice(40);

                    int sendt = 0;
                    do
                    {
                        ReadOnlyMemory<byte> sendBlock = memory.Slice(sendt);
                        int remaining = await connection.Source.SendAsync(sendBlock, SocketFlags.None).ConfigureAwait(false);
                        if (remaining == 0) break;

                        sendt += remaining;
                    } while (sendt < memory.Length);
                }
                catch (Exception ex)
                {
                    connection.ReadPacket.Flags = LinkerSrcProxyFlags.Rst;
                    connection.ReadPacket.TotalLength = 40;
                    connection.ReadPacket.Length = 44;
                    await callback.Callback(connection.ReadPacket).ConfigureAwait(false);

                    connections.TryRemove(key, out _);
                    connection.Disponse();

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                }
            }
        }
        private async Task HandleSyn(ConnectionKey key)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024 + 40 + 4);
            try
            {
                Socket source = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await source.ConnectAsync(new IPEndPoint(NetworkHelper.ToIP(key.dstAddr), key.dstPort)).WaitAsync(TimeSpan.FromMilliseconds(5000));

                ConnectionState state = new ConnectionState
                {
                    Source = source,
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
                state.ReadPacket.SrcAddr = key.dstAddr;
                state.ReadPacket.DstAddr = key.srcAddr;
                state.ReadPacket.IPChecksum = 1;

                state.ReadPacket.SrcPort = key.dstPort;
                state.ReadPacket.DstPort = key.srcPort;
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

                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory(44), SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    state.ReadPacket.Flags = LinkerSrcProxyFlags.Psh;
                    state.ReadPacket.TotalLength = bytesRead + 40;
                    state.ReadPacket.Length = bytesRead + 44;
                    await callback.Callback(state.ReadPacket).ConfigureAwait(false);
                }
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

                    connection.Disponse();
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private void HandleSynAck(ConnectionKey key)
        {
            if (connections.TryGetValue(key, out ConnectionState connection))
            {
                connection.Tcs?.SetResult();
            }
        }
        private void HandleRst(ConnectionKey key)
        {
            if (connections.TryRemove(key, out ConnectionState connection))
            {
                connection.Disponse();
            }
        }

        public unsafe bool Read(ReadOnlyMemory<byte> packet, ref bool send, ref bool writeBack)
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
                    if (srcMap.TryGetValue((srcProxyPacket.SrcAddr, srcProxyPacket.DstPort), out SrcCacheInfo cache))
                    {
                        if (srcProxyPacket.TcpFlagFin || srcProxyPacket.TcpFlagRst) cache.Fin = true;
                        //3、10.18.18.2:33333->10.18.18.2:22222 改为 10.18.18.3:5201->10.18.18.2:11111 
                        srcProxyPacket.DstAddr = srcProxyPacket.SrcAddr;
                        srcProxyPacket.DstPort = cache.SrcPort;
                        srcProxyPacket.SrcAddr = cache.DstAddr;
                        srcProxyPacket.SrcPort = cache.DstPort;
                        srcProxyPacket.IPChecksum = 0; //需要重新计算IP头校验和
                        srcProxyPacket.PayloadChecksum = 0; //需要重新计算TCP校验和
                        send = false; //拦截，不发送
                        writeBack = true; //写回网卡
                    }
                }
                else //从访问端来的
                {
                    (uint srcIp, ushort srcPort) key = (srcProxyPacket.SrcAddr, srcProxyPacket.SrcPort);
                    //(10.18.18.2,11111)、取不到是SYN包则建立映射，不是SYN包则继续走原路
                    if (srcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        if (srcProxyPacket.IsOnlySyn == false) return true; //往下走
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
                        srcMap.AddOrUpdate((srcProxyPacket.SrcAddr, cache.SrcPort), cache, (a, b) => cache);
                        srcMap.AddOrUpdate((srcProxyPacket.SrcAddr, cache.NewPort), cache, (a, b) => cache);
                    }
                    if (srcProxyPacket.TcpFlagFin || srcProxyPacket.TcpFlagRst) cache.Fin = true;
                    //2、10.18.18.2:11111->10.18.18.3:5201 改为 10.18.18.0:22222->10.18.18.2:33333 包括[SYN/PSH+ACK/ACK/FIN/RST]的任意包
                    srcProxyPacket.DstAddr = srcProxyPacket.SrcAddr;
                    srcProxyPacket.DstPort = proxyPort;
                    srcProxyPacket.SrcAddr = proxySrc;
                    srcProxyPacket.SrcPort = cache.NewPort;
                    srcProxyPacket.IPChecksum = 0; //需要重新计算IP头校验和
                    srcProxyPacket.PayloadChecksum = 0;//需要重新计算TCP校验和
                    send = false;//拦截，不发送
                    writeBack = true;//写回网卡
                }
                return false; //不再往下走
            }

        }

        private void SrcMapClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                foreach (var item in srcMap.Where(c => c.Value.Fin && Environment.TickCount64 - c.Value.LastTime > 60 * 1000).Select(c => c.Key).ToList())
                {
                    srcMap.TryRemove(item, out _);
                }
            }, 30000);
        }

        struct ConnectionKey
        {
            public uint srcAddr;
            public ushort srcPort;
            public uint dstAddr;
            public ushort dstPort;
        }
        sealed class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
        {
            public bool Equals(ConnectionKey x, ConnectionKey y)
            {
                return (x.srcAddr, x.srcPort, x.dstAddr, x.dstPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort)
                    || (x.dstAddr, x.dstPort, x.srcAddr, x.srcPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort);
            }

            public int GetHashCode(ConnectionKey obj)
            {
                return (int)obj.srcAddr ^ obj.srcPort ^ (int)obj.dstAddr ^ obj.dstPort;
            }
        }
        sealed class ConnectionState
        {
            public Socket Source { get; init; }
            public LinkerSrcProxyReadPacket ReadPacket { get; init; }

            public TaskCompletionSource Tcs { get; init; }

            public void Disponse()
            {
                Source?.SafeClose();
                ReadPacket?.Dispose();
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
            public readonly byte TcpFlag => *(ptr + IPHeadLength + 13);
            public readonly bool TcpFlagFin => (TcpFlag & 0b000001) != 0;
            public readonly bool TcpFlagSyn => (TcpFlag & 0b000010) != 0;
            public readonly bool TcpFlagRst => (TcpFlag & 0b000100) != 0;
            public readonly bool TcpFlagPsh => (TcpFlag & 0b001000) != 0;
            public readonly bool TcpFlagAck => (TcpFlag & 0b010000) != 0;
            public readonly bool TcpFlagUrg => (TcpFlag & 0b100000) != 0;

            public readonly bool IsPshAck => TcpFlagPsh && TcpFlagAck;
            public readonly bool IsOnlyAck => TcpFlag == 0b00010000;
            public readonly bool IsOnlySyn => TcpFlag == 0b00000010;
            public readonly bool IsSynAck => TcpFlag == 0b00010010;

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

        readonly unsafe struct LinkerSrcProxyWritePacket : IDisposable
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

            public readonly uint Seq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 4));
            public readonly uint Cq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 8));


            public byte DataOffset => (byte)((*(PayloadPtr + 12) >> 4) & 0b1111);
            public int HeadLength => DataOffset * 4;
            public readonly LinkerSrcProxyFlags Flag => (LinkerSrcProxyFlags)(*(ptr + IPHeadLength + 13));


            public unsafe LinkerSrcProxyWritePacket(ReadOnlyMemory<byte> packet)
            {
                handle = packet.Pin();
                this.ptr = (byte*)handle.Pointer;
            }

            readonly MemoryHandle handle;
            public void Dispose()
            {
                handle.Dispose();
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
                *(ptr + 6) = (byte)((*ptr & 0b00011111) | ((value & 0b111) << 5));
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
                *(PayloadPtr + 12) = (byte)((*ptr & 0b00001111) | ((value & 0b1111) << 4));
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
                *(PayloadPtr + 12) = (byte)((*ptr & 0b11110001) | ((value & 0b111) << 1));
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
            MemoryHandle handle = buffer.AsMemory().Pin();
            this.ptr = (byte*)handle.Pointer + 4;
        }

        MemoryHandle handle;
        public void Dispose()
        {
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
    }
    public interface ILinkerSrcProxyCallback
    {
        public Task Callback(LinkerSrcProxyReadPacket packet);
        public bool Callback(uint ip);
    }
}
