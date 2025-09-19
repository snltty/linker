using linker.libs;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace linker.tun
{
    /// <summary>
    /// 伪造ACK操作类
    /// </summary>
    public unsafe sealed class FakeAckTransfer
    {
        private readonly ConcurrentDictionary<FaceAckKey, int> dic = new(new FackAckKeyComparer());

        /// <summary>
        /// 发起方
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <returns></returns>
        public void Read(ReadOnlyMemory<byte> packet)
        {
            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket originPacket = new(ptr);
                if (originPacket.Version != 4 || originPacket.Protocol != ProtocolType.Tcp)
                {
                    return;
                }
                if (originPacket.TcpFlagFin || originPacket.TcpFlagRst)
                {
                    FaceAckKey key = new() { srcAddr = originPacket.SrcAddr, srcPort = originPacket.SrcPort, dstAddr = originPacket.DstAddr, dstPort = originPacket.DstPort };
                    dic.TryRemove(key, out _);
                }
            }
        }
        /// <summary>
        /// 接收方
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <returns></returns>
        public void Write(ReadOnlyMemory<byte> packet, long bufferFree)
        {
            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket originPacket = new(ptr);
                if (originPacket.Version != 4 || originPacket.Protocol != ProtocolType.Tcp)
                {
                    return;
                }
                FaceAckKey key = new() { srcAddr = originPacket.SrcAddr, srcPort = originPacket.SrcPort, dstAddr = originPacket.DstAddr, dstPort = originPacket.DstPort };

                if (originPacket.IsPshAck || originPacket.IsOnlyAck)
                {

                    if (dic.TryGetValue(key, out int wins) && originPacket.Window > 0)
                    {
                        ushort win = (ushort)Math.Max(Math.Min(bufferFree / wins, 65535), 4);
                        originPacket.WriteWindow(ptr, win);
                    }

                }
                else if (originPacket.IsOnlySyn || originPacket.IsSynAck)
                {
                    int windowScale = originPacket.FindWindowScale(ptr);
                    dic.AddOrUpdate(key, windowScale, (a, b) => windowScale);
                }
                else if (originPacket.TcpFlagFin || originPacket.TcpFlagRst)
                {
                    dic.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// 四元组缓存key
        /// </summary>
        struct FaceAckKey
        {
            public uint srcAddr;
            public ushort srcPort;
            public uint dstAddr;
            public ushort dstPort;
        }
        /// <summary>
        /// 四元组缓存key比较器
        /// </summary>
        sealed class FackAckKeyComparer : IEqualityComparer<FaceAckKey>
        {
            public bool Equals(FaceAckKey x, FaceAckKey y)
            {
                return (x.srcAddr, x.srcPort, x.dstAddr, x.dstPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort)
                    || (x.dstAddr, x.dstPort, x.srcAddr, x.srcPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort);
            }

            public int GetHashCode(FaceAckKey obj)
            {
                return (int)obj.srcAddr ^ obj.srcPort ^ (int)obj.dstAddr ^ obj.dstPort;
            }
        }

        /// <summary>
        /// 数据包解析
        /// </summary>
        readonly unsafe struct FakeAckPacket
        {
            private readonly byte* ptr;

            /// <summary>
            /// 协议版本
            /// </summary>
            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly ProtocolType Protocol => (ProtocolType)(*(ptr + 9));

            /// <summary>
            /// 源地址
            /// </summary>
            public readonly uint SrcAddr => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
            /// <summary>
            /// 源端口
            /// </summary>
            public readonly ushort SrcPort => BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + IPHeadLength));
            /// <summary>
            /// 目的地址
            /// </summary>
            public readonly uint DstAddr => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
            /// <summary>
            /// 目标端口
            /// </summary>
            public readonly ushort DstPort => BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + IPHeadLength + 2));

            /// <summary>
            /// IP头长度
            /// </summary>
            public readonly int IPHeadLength => (*ptr & 0b1111) * 4;
            /// <summary>
            /// 窗口大小
            /// </summary>
            public readonly ushort Window => BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + IPHeadLength + 14));

            /// <summary>
            /// TCP Flag
            /// </summary>
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
            /// 加载TCP/IP包，必须是一个完整的TCP/IP包
            /// </summary>
            /// <param name="ptr">一个完整的TCP/IP包</param>
            public FakeAckPacket(byte* ptr)
            {
                this.ptr = ptr;
            }

            public int FindWindowScale(byte* ipPtr)
            {
                //指针移动到TCP头开始位置
                byte* tcpPtr = ipPtr + ((*ipPtr & 0b1111) * 4);

                //tcp头固定20，所以option从这里开始
                int index = 20;
                //tcp头结束位置，就是option结束位置
                int end = (*(tcpPtr + 12) >> 4) * 4;
                while (index < end)
                {
                    byte kind = *(tcpPtr + index);
                    //EOF结束符
                    if (kind == 0)
                    {
                        break;
                    }

                    byte length = *(tcpPtr + index + 1);
                    //NOP 空选项
                    if (kind == 1)
                    {
                        index++;
                        continue;
                    }
                    //Window Scale 1kind 1length 1shiftCount
                    else if (kind == 3 && length == 3)
                    {
                        return *(tcpPtr + index + 2);
                    }
                    index += length;
                }
                return 0;
            }
            public int WriteWindowScale(byte* ipPtr, byte windowScale = 7)
            {
                //指针移动到TCP头开始位置
                byte* tcpPtr = ipPtr + ((*ipPtr & 0b1111) * 4);

                //tcp头固定20，所以option从这里开始
                int index = 20;
                //tcp头结束位置，就是option结束位置
                int end = (*(tcpPtr + 12) >> 4) * 4;
                while (index < end)
                {
                    byte kind = *(tcpPtr + index);
                    //EOF结束符
                    if (kind == 0)
                    {
                        break;
                    }

                    byte length = *(tcpPtr + index + 1);
                    //NOP 空选项
                    if (kind == 1)
                    {
                        index++;
                        continue;
                    }
                    //Window Scale 1kind 1length 1shiftCount
                    else if (kind == 3 && length == 3)
                    {
                        if (*(tcpPtr + index + 2) < windowScale)
                        {
                            *(tcpPtr + index + 2) = windowScale;
                            ChecksumHelper.Checksum(ipPtr, false, true);
                        }
                        return *(tcpPtr + index + 2);
                    }
                    index += length;
                }
                return 0;
            }
            public void WriteWindow(byte* ipPtr, ushort window)
            {
                *(ushort*)(ipPtr + ((*ipPtr & 0b1111) * 4) + 14) = BinaryPrimitives.ReverseEndianness(Math.Max(window, (ushort)8));
                ChecksumHelper.Checksum(ipPtr, false, true);
            }
        }
    }
}
