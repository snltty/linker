using linker.libs;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace linker.tun
{
    
    public unsafe sealed class FakeAckTransfer
    {
        private readonly ConcurrentDictionary<FackAckKey, FackAckState> dic = new(new FackAckKeyComparer());

        /// <summary>
        /// 发起方
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <param name="fakeBuffer">一个能容纳ACK包的缓冲区，如果需要伪造ACK则写入到这里</param>
        /// <param name="fakeLength">ack包长度</param>
        /// <returns>是否丢包</returns>
        public bool Read(ReadOnlyMemory<byte> packet, ReadOnlyMemory<byte> fakeBuffer, out ushort fakeLength)
        {
            fakeLength = 0;

            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket originPacket = new FakeAckPacket(ptr);
                if (originPacket.Version != 4 || originPacket.Protocol != ProtocolType.Tcp || originPacket.IsOnlySyn)
                {
                    return false;
                }

                FackAckKey key = new() { srcAddr = originPacket.SrcAddr, srcPort = originPacket.SrcPort, dstAddr = originPacket.DstAddr, dstPort = originPacket.DstPort };
                //发送建立连接，由对方回一个ack，所以这边可以直接设置为丢弃ack
                if (originPacket.TcpFlagSyn && originPacket.TcpFlagAck)
                {
                    FackAckState _state = new() { Seq = originPacket.Seq + 1, DropAck = true };
                    dic.AddOrUpdate(key, _state, (a, b) => _state);
                    return false;
                }
                //断开连接
                if (originPacket.TcpFlagFin || originPacket.TcpFlagRst)
                {
                    dic.TryRemove(key, out _);
                    return false;
                }
                if (dic.TryGetValue(key, out FackAckState state))
                {
                    //已经发送ack，开始丢弃ack，排除捎带机制(就是在ACK中携带数据，这些包不能丢)
                    if (originPacket.IsOnlyAck)
                    {
                        return state.SetDrop(true) && originPacket.TcpPayloadLength == 0;
                    }
                    //发送数据
                    if (originPacket.IsPshAck)
                    {
                        fixed (byte* pptr = fakeBuffer.Span)
                        {
                            //伪造ACK包
                            fakeLength = originPacket.ToAck(state.Seq, pptr);
                        }
                    }
                }

            }
            return false;
        }
        /// <summary>
        /// 接收方，只需要在合适的时候建立状态和删除状态，其它的都交给发起方处理
        /// </summary>
        /// <param name="packet">一个完整的TCP/IP包</param>
        /// <returns></returns>
        public void Write(ReadOnlyMemory<byte> packet)
        {
            fixed (byte* ptr = packet.Span)
            {
                FakeAckPacket fakeAckTCPPacket = new FakeAckPacket(ptr);
                if (fakeAckTCPPacket.Version != 4 || fakeAckTCPPacket.Protocol != ProtocolType.Tcp || fakeAckTCPPacket.IsOnlySyn)
                {
                    return;
                }
                FackAckKey key = new() { srcAddr = fakeAckTCPPacket.SrcAddr, srcPort = fakeAckTCPPacket.SrcPort, dstAddr = fakeAckTCPPacket.DstAddr, dstPort = fakeAckTCPPacket.DstPort };
                //收到连接连接
                if (fakeAckTCPPacket.TcpFlagSyn && fakeAckTCPPacket.TcpFlagAck)
                {
                    FackAckState state = new() { Seq = fakeAckTCPPacket.Seq + 1 };
                    dic.AddOrUpdate(key, state, (a, b) => state);
                }
                //断开连接
                else if (fakeAckTCPPacket.TcpFlagFin || fakeAckTCPPacket.TcpFlagRst)
                {
                    dic.TryRemove(key, out _);
                }
            }
        }

        sealed class FackAckState
        {
            public uint Seq { get; set; }
            public bool DropAck { get; set; }
            public bool SetDrop(bool value)
            {
                bool drop = DropAck;
                DropAck = value;
                return drop;
            }
        }
        struct FackAckKey
        {
            public uint srcAddr;
            public ushort srcPort;
            public uint dstAddr;
            public ushort dstPort;
        }
        sealed class FackAckKeyComparer : IEqualityComparer<FackAckKey>
        {
            public bool Equals(FackAckKey x, FackAckKey y)
            {
                return (x.srcAddr, x.srcPort, x.dstAddr, x.dstPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort)
                    || (x.dstAddr, x.dstPort, x.srcAddr, x.srcPort) == (y.srcAddr, y.srcPort, y.dstAddr, y.dstPort);
            }

            public int GetHashCode(FackAckKey obj)
            {
                return (int)obj.srcAddr ^ obj.srcPort ^ (int)obj.dstAddr ^ obj.dstPort;
            }
        }

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
            public readonly int TcpPayloadLength => BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 2)) - (*ptr & 0b1111) * 4 - (*(ptr + 12) >> 4) * 4;

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
            /// <param name="dstPtr">目标内存</param>
            /// <returns></returns>
            public readonly unsafe ushort ToAck(uint seq, byte* dstPtr)
            {
                int _ipHeadLength = (*ptr & 0b1111) * 4;
                int _tcpHeaderLength = (*(ptr + _ipHeadLength + 12) >> 4) * 4;
                int _headerLength = _ipHeadLength + _tcpHeaderLength;
                new Span<byte>(ptr, _headerLength).CopyTo(new Span<byte>(dstPtr, _headerLength));

                byte* tcpPtr = dstPtr + _ipHeadLength;

                ushort totalLength = BinaryPrimitives.ReverseEndianness(*(ushort*)(dstPtr + 2));
                int ipHeaderLength = (*dstPtr & 0b1111) * 4;
                int tcpHeaderLength = (*(tcpPtr + 12) >> 4) * 4;
                uint payloadLength = (uint)(totalLength - ipHeaderLength - tcpHeaderLength);
                totalLength = (ushort)(ipHeaderLength + tcpHeaderLength);

                //交换地址和端口
                (*(uint*)(dstPtr + 16), *(uint*)(dstPtr + 12)) = (*(uint*)(dstPtr + 12), *(uint*)(dstPtr + 16));
                (*(ushort*)(tcpPtr + 2), *(ushort*)(tcpPtr)) = (*(ushort*)(tcpPtr), *(ushort*)(tcpPtr + 2));


                //设置总长度 = IP头长度 + TCP头长度
                *(ushort*)(dstPtr + 2) = BinaryPrimitives.ReverseEndianness(totalLength);

                //重置分片相关信息
                *(ushort*)(dstPtr + 4) = 0; // 清除分片偏移和标志
                *(ushort*)(dstPtr + 6) = 0; // 清除更多分片标志

                //源序列号
                uint _seq = BinaryPrimitives.ReverseEndianness(*(uint*)(tcpPtr + 4));
                //新确认号 = 序列号+ 数据长度
                uint cq = _seq + payloadLength;

                //设置序列号
                *(uint*)(tcpPtr + 4) = BinaryPrimitives.ReverseEndianness(seq);
                //设置确认号
                *(uint*)(tcpPtr + 8) = BinaryPrimitives.ReverseEndianness(cq);

                //设置TCP标志位为ACK，其他标志位清除
                *(tcpPtr + 13) = 0b00010000;

                //设置窗口大小
                *(ushort*)(tcpPtr + 14) = BinaryPrimitives.ReverseEndianness((ushort)65535);

                //计算校验和
                ChecksumHelper.Checksum(dstPtr, totalLength);

                //只需要IP头+TCP头
                return totalLength;
            }
        }
    }
}
