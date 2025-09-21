using System;
using System.Buffers.Binary;
using System.Net.Sockets;
namespace linker.libs
{
    public sealed class ChecksumHelper
    {
        /// <summary>
        /// 清空IP包的校验和
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="ipHeader">是否情况IP头校验和</param>
        /// <param name="payload">是否清空荷载协议校验和</param>
        public static unsafe void ClearChecksum(byte* ptr, bool ipHeader = true, bool payload = true)
        {
            byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);
            byte* packetPtr = ptr + ipHeaderLength;

            if (ipHeader)
            {
                *(ushort*)(ptr + 10) = 0;
            }
            if (payload)
            {
                int index = (ProtocolType)(*(ptr + 9)) switch
                {
                    ProtocolType.Icmp => 2,
                    ProtocolType.Tcp => 16,
                    ProtocolType.Udp => 6,
                    _ => -1,
                };
                if (index > 0)
                {
                    *(ushort*)(packetPtr + index) = 0;
                }
            }
        }


        /// <summary>
        /// 计算IP包的校验和，当校验和为0时才计算
        /// </summary>
        /// <param name="packet">一个完整的IP包</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void ChecksumWithZero(ReadOnlyMemory<byte> packet, bool ipHeader = true, bool payload = true)
        {
            ChecksumWithZero(packet.Span, ipHeader, payload);
        }
        /// <summary>
        /// 计算IP包的校验和，当校验和为0时才计算
        /// </summary>
        /// <param name="packet">一个完整的IP包</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void ChecksumWithZero(ReadOnlySpan<byte> packet, bool ipHeader = true, bool payload = true)
        {
            fixed (byte* ptr = packet)
            {
                ChecksumWithZero(ptr, ipHeader, payload);
            }
        }
        /// <summary>
        /// 计算IP包的校验和，当校验和为0时才计算
        /// </summary>
        /// <param name="ptr">IP包指针</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void ChecksumWithZero(byte* ptr, bool ipHeader = true, bool payload = true)
        {
            byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);
            byte* packetPtr = ptr + ipHeaderLength;

            ipHeader = ipHeader && *(ushort*)(ptr + 10) == 0;
            payload = payload && ((ProtocolType)(*(ptr + 9)) switch
            {
                ProtocolType.Icmp => *(ushort*)(packetPtr + 2) == 0,
                ProtocolType.Tcp => *(ushort*)(packetPtr + 16) == 0,
                ProtocolType.Udp => *(ushort*)(packetPtr + 6) == 0,
                _ => false,
            });
            if (ipHeader || payload)
                Checksum(ptr, ipHeader, payload);
        }

        /// <summary>
        /// 计算IP包的校验和
        /// </summary>
        /// <param name="packet">一个完整的IP包</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void Checksum(ReadOnlyMemory<byte> packet, bool ipHeader = true, bool payload = true)
        {
            Checksum(packet.Span, ipHeader, payload);
        }
        /// <summary>
        /// 计算IP包的校验和
        /// </summary>
        /// <param name="packet">一个完整的IP包</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void Checksum(ReadOnlySpan<byte> packet, bool ipHeader = true, bool payload = true)
        {
            fixed (byte* ptr = packet)
            {
                Checksum(ptr, ipHeader, payload);
            }
        }
        /// <summary>
        /// 计算IP包的校验和
        /// </summary>
        /// <param name="ptr">IP包指针</param>
        /// <param name="ipHeader">是否计算IP头校验和</param>
        /// <param name="payload">是否计算荷载协议校验和</param>
        public static unsafe void Checksum(byte* ptr, bool ipHeader = true, bool payload = true)
        {
            byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);
            byte* packetPtr = ptr + ipHeaderLength;
            uint totalLength = BinaryPrimitives.ReverseEndianness(*(ushort*)(ptr + 2));
            uint packetLength = totalLength - ipHeaderLength;

            if (ipHeader)
            {
                //重新计算IP头校验和
                *(ushort*)(ptr + 10) = 0;
                *(ushort*)(ptr + 10) = Checksum((ushort*)ptr, ipHeaderLength);
            }


            if (payload)
            {
                ProtocolType protocol = (ProtocolType)(*(ptr + 9));
                switch (protocol)
                {
                    case ProtocolType.Tcp:
                        {
                            *(ushort*)(packetPtr + 16) = 0;
                            ulong sum = PseudoHeaderSum(ptr, packetLength);
                            *(ushort*)(packetPtr + 16) = Checksum((ushort*)(packetPtr), packetLength, sum);
                        }
                        break;
                    case ProtocolType.Udp:
                        {
                            *(ushort*)(packetPtr + 6) = 0;
                            ulong sum = PseudoHeaderSum(ptr, packetLength);
                            *(ushort*)(packetPtr + 6) = Checksum((ushort*)(packetPtr), packetLength, sum);
                        }
                        break;
                    case ProtocolType.Icmp:
                        {
                            *(ushort*)(packetPtr + 2) = 0;
                            *(ushort*)(packetPtr + 2) = Checksum((ushort*)(packetPtr), packetLength);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="addr">包头开始位置</param>
        /// <param name="length">计算长度，不同协议不同长度，请自己斟酌</param>
        /// <param name="pseudoHeaderSum">伪头部和，默认0不需要伪头部和</param>
        /// <returns></returns>
        private static unsafe ushort Checksum(ushort* addr, uint length, ulong pseudoHeaderSum = 0)
        {
            //每两个字节为一个数，之和
            while (length > 1)
            {
                pseudoHeaderSum += (ushort)((*addr >> 8) + (*addr << 8));
                addr++;
                length -= 2;
            }
            //奇数字节末尾补零
            if (length > 0) pseudoHeaderSum += (ushort)((*addr) << 8);
            //溢出处理
            while ((pseudoHeaderSum >> 16) != 0) pseudoHeaderSum = (pseudoHeaderSum & 0xffff) + (pseudoHeaderSum >> 16);
            //取反
            return BinaryPrimitives.ReverseEndianness((ushort)(~pseudoHeaderSum));
        }
        /// <summary>
        /// 计算伪头部和，如TCP/UDP校验和需要一个伪头部
        /// </summary>
        /// <param name="addr">IP包头开始</param>
        /// <param name="length">TCP/UDP长度</param>
        /// <returns></returns>
        private static unsafe ulong PseudoHeaderSum(byte* addr, uint length)
        {
            uint sum = 0;
            //源IP+目的IP
            for (byte i = 12; i < 20; i += 2) sum += (uint)((*(addr + i) << 8) | *(addr + i + 1));
            //协议
            sum += *(addr + 9);
            //协议内容长度
            sum += length;
            return sum;
        }
    }
}
