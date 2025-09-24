using linker.libs;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace linker.nat
{

    /// <summary>
    /// 伪造ACK操作类
    /// </summary>
    public unsafe sealed class FakeAckTransfer
    {
        private readonly ConcurrentDictionary<FaceAckKey, FackAckState> dic = new(new FackAckKeyComparer());

        /// <summary>
        /// 发起方
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <param name="fakeBuffer">一个能容纳ACK包的缓冲区，如果需要伪造ACK则写入到这里</param>
        /// <param name="bufferFree">缓冲区可用字节数，会根据这个来计算ack的窗口大小</param>
        /// <param name="fakeLength">ack包长度</param>
        /// <returns>是否丢包</returns>
        public bool Read(ReadOnlyMemory<byte> packet, ReadOnlyMemory<byte> fakeBuffer, long bufferFree, out ushort fakeLength)
        {
            fakeLength = 0;

            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket originPacket = new(ptr);
                if (originPacket.Protocol != ProtocolType.Tcp)
                {
                    return false;
                }

                FaceAckKey key = new() { srcAddr = originPacket.SrcAddr, srcPort = originPacket.SrcPort, dstAddr = originPacket.DstAddr, dstPort = originPacket.DstPort };

                if ((originPacket.IsOnlyAck || originPacket.IsPshAck) && dic.TryGetValue(key, out FackAckState state))
                {
                    if (originPacket.TcpPayloadLength == 0)
                    {
                        return state.Ack++ > 0;
                    }

                    //originPacket.ClearConfirmNum(ptr);

                    fixed (byte* pptr = fakeBuffer.Span)
                    {
                        ushort win = (ushort)Math.Max(Math.Min(bufferFree / state.WindowScale, 65535), 4);
                        fakeLength = originPacket.ToAck(state.Seq, win, pptr);
                        /*
                        if (new FakeAckPacket(pptr).Cq <= state.Cq)
                        {
                            fakeLength = 0;
                        }
                        */
                    }
                }
                else if (originPacket.TcpFlagFin || originPacket.TcpFlagRst)
                {
                    dic.TryRemove(key, out _);
                }
            }
            return false;
        }
        /// <summary>
        /// 接收方
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <returns></returns>
        public void Write(ReadOnlyMemory<byte> packet)
        {
            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket originPacket = new(ptr);
                if (originPacket.Protocol != ProtocolType.Tcp)
                {
                    return;
                }
                FaceAckKey key = new() { srcAddr = originPacket.SrcAddr, srcPort = originPacket.SrcPort, dstAddr = originPacket.DstAddr, dstPort = originPacket.DstPort };

                /*
                //更新序列号
                if (originPacket.TcpFlagAck && dic.TryGetValue(key, out FackAckState state))
                {
                    state.Cq = originPacket.Cq;

                    if (originPacket.TcpPayloadLength > 0)
                    {
                        state.Seq = originPacket.Seq + (uint)originPacket.TcpPayloadLength;
                    }
                }
                else*/
                if (originPacket.IsOnlySyn || originPacket.IsSynAck)
                {
                    FackAckState state = new()
                    {
                        Ack = (ulong)(originPacket.IsOnlySyn ? 1 : 0),
                        Seq = originPacket.Seq + 1,
                        WindowScale = originPacket.FindWindowScale(ptr + originPacket.IPHeadLength)
                    };
                    dic.AddOrUpdate(key, state, (a, b) => state);
                }
                else if (originPacket.TcpFlagFin || originPacket.TcpFlagRst)
                {
                    dic.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// 状态
        /// </summary>
        sealed class FackAckState
        {
            public ulong Ack { get; set; }
            public uint Seq { get; set; }
            public uint Cq { get; set; }
            public int WindowScale { get; set; }
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
            /// 序列号
            /// </summary>
            public readonly uint Seq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 4));
            /// <summary>
            /// 确认号
            /// </summary>
            public readonly uint Cq => BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + IPHeadLength + 8));

            /// <summary>
            /// TCP负载长度
            /// </summary>
            public readonly int TcpPayloadLength
            {
                get
                {
                    int ipHeadLength = (*ptr & 0b1111) * 4;
                    int tcpHeaderLength = (*(ptr + ipHeadLength + 12) >> 4) * 4;
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 2)) - ipHeadLength - tcpHeaderLength;
                }
            }

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

            /// <summary>
            /// 制作一个ACK包
            /// </summary>
            /// <param name="seq">给定一个序列号，可以从syn+ack包中+1获得</param>
            /// <param name="winSize">窗口大小</param>
            /// <param name="ipPtr">目标内存</param>
            /// <returns></returns>
            public readonly unsafe ushort ToAck(uint seq, ushort winSize, byte* ipPtr)
            {
                //复制一份IP+TCP头部
                int ipHeaderLength = (*ptr & 0b1111) * 4;
                int tcpHeaderLength = (*(ptr + ipHeaderLength + 12) >> 4) * 4;
                ushort totalLength = BinaryPrimitives.ReverseEndianness(*(ushort*)(ipPtr + 2));
                uint payloadLength = (uint)(totalLength - ipHeaderLength - tcpHeaderLength);

                new Span<byte>(ptr, ipHeaderLength + tcpHeaderLength).CopyTo(new Span<byte>(ipPtr, ipHeaderLength + tcpHeaderLength));

                //TCP头指针
                byte* tcpPtr = ipPtr + ipHeaderLength;

                //如果有时间戳，就填充时间戳选项
                //FullOptionTimestamp(tcpPtr);
                *(tcpPtr + 12) = 0b01010000;
                //重新计算头部长度
                tcpHeaderLength = (*(tcpPtr + 12) >> 4) * 4;
                totalLength = (ushort)(ipHeaderLength + tcpHeaderLength);

                //交换地址和端口
                (*(uint*)(ipPtr + 16), *(uint*)(ipPtr + 12)) = (*(uint*)(ipPtr + 12), *(uint*)(ipPtr + 16));
                (*(ushort*)(tcpPtr + 2), *(ushort*)(tcpPtr)) = (*(ushort*)(tcpPtr), *(ushort*)(tcpPtr + 2));

                //设置总长度
                *(ushort*)(ipPtr + 2) = BinaryPrimitives.ReverseEndianness(totalLength);

                //重置分片相关信息
                *(ushort*)(ipPtr + 4) = 0; // 清除分片偏移和标志
                *(ushort*)(ipPtr + 6) = 0; // 清除更多分片标志

                //源序列号
                uint _seq = BinaryPrimitives.ReverseEndianness(*(uint*)(tcpPtr + 4));
                //设置序列号
                *(uint*)(tcpPtr + 4) = BinaryPrimitives.ReverseEndianness(seq);
                //设置确认号
                *(uint*)(tcpPtr + 8) = BinaryPrimitives.ReverseEndianness(_seq + payloadLength);

                //设置TCP标志位为ACK，其他标志位清除
                *(tcpPtr + 13) = 0b00010000;

                //设置窗口大小
                *(ushort*)(tcpPtr + 14) = BinaryPrimitives.ReverseEndianness(Math.Max(winSize, (ushort)8));

                //计算校验和
                ChecksumHelper.Checksum(ipPtr);

                //只需要IP头+TCP头
                return totalLength;
            }

            public void ClearConfirmNum(byte* ipPtr)
            {
                byte* tcpPtr = ipPtr + (*ptr & 0b1111) * 4;

                *(tcpPtr + 13) = 0b00011000;
                *(uint*)(tcpPtr + 8) = 0;
                //计算校验和
                ChecksumHelper.Checksum(ipPtr);
            }

            /// <summary>
            /// 从TCP的SYN包或SYN+ACK包中，获取窗口缩放比例
            /// </summary>
            /// <param name="ipPtr">一个完整TCP/IP包</param>
            /// <returns></returns>
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
                        byte shiftCount = *(tcpPtr + index + 2);
                        return shiftCount > 14 ? 1 : 1 << shiftCount;
                    }
                    index += length;
                }
                return 1;
            }
        }
    }


    public unsafe sealed class FakeAckTransfer1
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