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

        private ConcurrentDictionary<(uint srcIp, ushort srcPort), SrcCacheInfo> dic = new();
        private ConcurrentDictionary<(uint srcIp, ushort srcPort, uint dstIp, ushort dstPort), ConnectionState> connections = new();

        public LinkerSrcProxy()
        {
            Clear();
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
                _ = ReceiveTcp();

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
        private async Task ReceiveTcp()
        {
            int hashcode = listenSocketTcp.GetHashCode();
            try
            {
                while (true)
                {
                    Socket source = await listenSocketTcp.AcceptAsync();
                    IPEndPoint ep = source.RemoteEndPoint as IPEndPoint;
                    (uint srcIp, ushort srcPort) key = (NetworkHelper.ToValue(ep.Address), (ushort)ep.Port);
                    if (dic.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        source.SafeClose();
                        continue;
                    }
                    _ = Connect(source, cache);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                if (listenSocketTcp != null && listenSocketTcp.GetHashCode() == hashcode)
                    Shutdown();
            }
        }
        private async Task Connect(Socket source, SrcCacheInfo cache)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192 + 40 + 4);

            try
            {
                ConnectionState connection = new ConnectionState
                {
                    Source = source,
                    ReadPacket = new LinkerSrcProxyReadPacket(buffer)
                };
                connections.TryAdd((tunIp, cache.SrcPort, cache.DstIP, cache.DstPort), connection);

                connection.ReadPacket.SrcAddr = tunIp;
                connection.ReadPacket.SrcPort = cache.SrcPort;
                connection.ReadPacket.DstAddr = cache.DstIP;
                connection.ReadPacket.DstPort = cache.DstPort;
                connection.ReadPacket.Version = 4;
                connection.ReadPacket.Protocol = ProtocolType.Tcp;
                connection.ReadPacket.IPHeadLength = 20;
                connection.ReadPacket.Seq = 0;
                connection.ReadPacket.Cq = 0;

                connection.ReadPacket.Flag = LinkerSrcProxyFlags.Syn;
                connection.ReadPacket.TotalLength = 40;
                connection.ReadPacket.Length = 44;
                await callback.Callback(connection.ReadPacket);


                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory(44), SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    connection.ReadPacket.Flag = LinkerSrcProxyFlags.Psh;
                    connection.ReadPacket.TotalLength = bytesRead + 40;
                    connection.ReadPacket.Length = bytesRead + 44;
                    await callback.Callback(connection.ReadPacket);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                if (connections.TryRemove((tunIp, cache.SrcPort, cache.DstIP, cache.DstPort), out ConnectionState connection))
                {
                    connection.ReadPacket.Flag = LinkerSrcProxyFlags.Rst;
                    connection.ReadPacket.TotalLength = 40;
                    connection.ReadPacket.Length = 44;
                    await callback.Callback(connection.ReadPacket);
                    connection.Disponse();
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public void Shutdown()
        {
            listenSocketTcp?.SafeClose();
            listenSocketTcp = null;
        }

        /// <summary>
        /// 写入网卡之前
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="write"></param>
        public unsafe void Write(ReadOnlyMemory<byte> packet, ref bool write)
        {
            LinkerSrcProxyWritePacket writePacket = new LinkerSrcProxyWritePacket(packet);
            if (writePacket.Seq > 0 || writePacket.Cq > 0)
            {
                return;
            }
            write = false;

            switch (writePacket.Flag)
            {
                case LinkerSrcProxyFlags.Syn:
                    {
                        _ = ConnectAsync(writePacket.SrcAddr, writePacket.SrcPort, writePacket.DstPort, writePacket.DstPort);
                    }
                    break;
                case LinkerSrcProxyFlags.Rst:
                    {
                        if (connections.TryRemove((writePacket.SrcAddr, writePacket.SrcPort, writePacket.DstPort, writePacket.DstPort), out ConnectionState connection))
                        {
                            connection.Disponse();
                        }
                    }
                    break;
                case LinkerSrcProxyFlags.Psh:
                    {
                        if (connections.TryGetValue((writePacket.SrcAddr, writePacket.SrcPort, writePacket.DstPort, writePacket.DstPort), out ConnectionState connection))
                        {
                            connection.Source.Send(packet.Slice(40).Span, SocketFlags.None);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private async Task ConnectAsync(uint srcIp, ushort srcPort, uint dstIp, ushort dstPort)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192 + 40 + 4);
            try
            {
                Socket source = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await source.ConnectAsync(new IPEndPoint(NetworkHelper.ToIP(dstIp), dstPort));

                LoggerHelper.Instance.Warning($"src proxy connect {source.RemoteEndPoint}");

                ConnectionState connection = new ConnectionState
                {
                    Source = source,
                    ReadPacket = new LinkerSrcProxyReadPacket(buffer)
                };
                connections.TryAdd((srcIp, srcPort, dstIp, dstPort), connection);

                connection.ReadPacket.SrcAddr = srcIp;
                connection.ReadPacket.SrcPort = srcPort;
                connection.ReadPacket.DstAddr = dstIp;
                connection.ReadPacket.DstPort = dstPort;
                connection.ReadPacket.Version = 4;
                connection.ReadPacket.Protocol = ProtocolType.Tcp;
                connection.ReadPacket.IPHeadLength = 20;
                connection.ReadPacket.Seq = 0;
                connection.ReadPacket.Cq = 0;
                connection.ReadPacket.TotalLength = 40;
                connection.ReadPacket.Length = 44;

                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory(44), SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    connection.ReadPacket.Flag = LinkerSrcProxyFlags.Psh;
                    connection.ReadPacket.TotalLength = bytesRead + 40;
                    connection.ReadPacket.Length = bytesRead + 44;
                    await callback.Callback(connection.ReadPacket);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                if (connections.TryRemove((srcIp, srcPort, dstIp, dstPort), out ConnectionState connection))
                {
                    connection.ReadPacket.Flag = LinkerSrcProxyFlags.Rst;
                    connection.ReadPacket.TotalLength = 40;
                    connection.ReadPacket.Length = 44;
                    await callback.Callback(connection.ReadPacket);

                    connection.Disponse();
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 从网卡读取之后
        /// </summary>
        /// <param name="packet"></param>
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
                    if (dic.TryGetValue((srcProxyPacket.SrcAddr, srcProxyPacket.DstPort), out SrcCacheInfo cache))
                    {
                        if (srcProxyPacket.TcpFlagFin || srcProxyPacket.TcpFlagRst) cache.Fin = true;
                        //3、10.18.18.2:33333->10.18.18.2:22222 改为 10.18.18.3:5201->10.18.18.2:11111 
                        srcProxyPacket.DstAddr = srcProxyPacket.SrcAddr;
                        srcProxyPacket.DstPort = cache.SrcPort;
                        srcProxyPacket.SrcAddr = cache.DstIP;
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
                    if (dic.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        if (srcProxyPacket.IsOnlySyn == false) return true; //往下走
                        if (callback.Callback(srcProxyPacket.DstPort) == false) return true;//不支持代理
                        //1、10.18.18.2:11111->10.18.18.3:5201 [SYN] 新连接
                        cache = new SrcCacheInfo
                        {
                            DstIP = srcProxyPacket.DstAddr,
                            DstPort = srcProxyPacket.DstPort,
                            SrcPort = srcProxyPacket.SrcPort,
                            NewPort = ApplyNewPort() //随机新端口,比如 22222，windows某些版本不需要新端口，可以直接使用11111
                        };
                        //添加 (10.18.18.2,11111)、(10.18.18.2,22222) 作为key的缓存
                        dic.AddOrUpdate((srcProxyPacket.SrcAddr, cache.SrcPort), cache, (a, b) => cache);
                        dic.AddOrUpdate((srcProxyPacket.SrcAddr, cache.NewPort), cache, (a, b) => cache);
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
        private static ushort ApplyNewPort()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return (ushort)(socket.LocalEndPoint as IPEndPoint).Port;
        }

        private void Clear()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                foreach (var item in dic.Where(c => c.Value.Fin && Environment.TickCount64 - c.Value.LastTime > 60 * 1000).Select(c => c.Key).ToList())
                {
                    dic.TryRemove(item, out _);
                }
            }, 30000);
        }

        sealed class ConnectionState
        {
            public Socket Source { get; init; }
            public LinkerSrcProxyReadPacket ReadPacket { get; init; }

            public void Disponse()
            {
                Source?.SafeClose();
            }
        }
        sealed class SrcCacheInfo
        {
            public long LastTime { get; set; } = Environment.TickCount64;
            public uint DstIP { get; set; }
            public ushort DstPort { get; set; }
            public ushort SrcPort { get; set; }
            public ushort NewPort { get; set; }
            public bool Fin { get; set; }
        }
        readonly unsafe struct SrcProxyPacket
        {
            private readonly byte* ptr;

            /// <summary>
            /// 协议版本
            /// </summary>
            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly ProtocolType Protocol => (ProtocolType)(*(ptr + 9));

            /// <summary>
            /// IP头长度
            /// </summary>
            public readonly int IPHeadLength => (*ptr & 0b1111) * 4;
            /// <summary>
            /// IP包荷载数据指针，也就是TCP/UDP头指针
            /// </summary>
            public readonly byte* PayloadPtr => ptr + IPHeadLength;

            /// <summary>
            /// 源地址
            /// </summary>
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
            /// <summary>
            /// 源端口
            /// </summary>
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
            /// <summary>
            /// 目的地址
            /// </summary>
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

            /// <summary>
            /// 目标端口
            /// </summary>
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

            /// <summary>
            /// 加载TCP/IP包，必须是一个完整的TCP/IP包
            /// </summary>
            /// <param name="ptr">一个完整的TCP/IP包</param>
            public SrcProxyPacket(byte* ptr)
            {
                this.ptr = ptr;
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

            public readonly uint Seq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 4));
            public readonly uint Cq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 8));
            public readonly LinkerSrcProxyFlags Flag => (LinkerSrcProxyFlags)(*(ptr + IPHeadLength + 13));

            public unsafe LinkerSrcProxyWritePacket(ReadOnlyMemory<byte> packet)
            {
                fixed (byte* ptr = packet.Span)
                {
                    this.ptr = ptr;
                }
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
        public ProtocolType Protocol
        {
            get
            {
                return (ProtocolType)(*(ptr + 9));
            }
            set
            {
                (*(ptr + 9)) = (byte)value;
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
        public byte* PayloadPtr => ptr + IPHeadLength;

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
                return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 4));

            }
            set
            {
                *(uint*)(ptr + 4) = BinaryPrimitives.ReverseEndianness(value);
            }
        }
        public uint Cq
        {
            get
            {
                return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 8));
            }
            set
            {
                *(uint*)(ptr + 8) = BinaryPrimitives.ReverseEndianness(value);
            }
        }

        public LinkerSrcProxyFlags Flag
        {
            get
            {
                return (LinkerSrcProxyFlags)(*(ptr + IPHeadLength + 13));
            }
            set
            {
                *(ptr + IPHeadLength + 13) = (byte)value;
            }
        }

        public LinkerSrcProxyReadPacket(byte[] buffer)
        {
            Buffer = buffer;
            fixed (byte* ptr = buffer)
                this.ptr = ptr;
        }

    }

    public enum LinkerSrcProxyFlags : byte
    {
        Syn = 0b00000010,
        Rst = 0b00000100,
        Psh = 0b00001000,
    }
    public interface ILinkerSrcProxyCallback
    {
        public Task Callback(LinkerSrcProxyReadPacket packet);
        public bool Callback(uint ip);
    }
}
