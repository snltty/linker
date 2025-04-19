using System;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace linker.libs
{
    public sealed class ChecksumHelper
    {
        /// <summary>
        /// 计算IP包的校验和
        /// </summary>
        /// <param name="packet">一个完整的IP包</param>
        public static unsafe void Checksum(ReadOnlyMemory<byte> packet)
        {
            fixed (byte* ptr = packet.Span)
            {
                Checksum(ptr, packet.Length);
            }
        }
        /// <summary>
        /// 计算IP包的校验和
        /// </summary>
        /// <param name="ptr">IP包指针</param>
        /// <param name="length">IP包长度</param>
        public static unsafe void Checksum(byte* ptr, int length)
        {
            byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);
            //重新计算IP头校验和
            *(ushort*)(ptr + 10) = 0;
            *(ushort*)(ptr + 10) = Checksum((ushort*)ptr, ipHeaderLength);

            ProtocolType protocol = (ProtocolType)(*(ptr + 9));
            switch (protocol)
            {
                case ProtocolType.Tcp:
                    {
                        *(ushort*)(ptr + ipHeaderLength + 16) = 0;
                        ulong sum = PseudoHeaderSum(ptr, (uint)(length - ipHeaderLength));
                        *(ushort*)(ptr + ipHeaderLength + 16) = Checksum((ushort*)(ptr + ipHeaderLength), (uint)length - ipHeaderLength, sum);
                    }
                    break;
                case ProtocolType.Udp:
                    {
                        *(ushort*)(ptr + ipHeaderLength + 6) = 0;
                        ulong sum = PseudoHeaderSum(ptr, (uint)(length - ipHeaderLength));
                        *(ushort*)(ptr + ipHeaderLength + 6) = Checksum((ushort*)(ptr + ipHeaderLength), (uint)length - ipHeaderLength, sum);
                    }
                    break;
                case ProtocolType.Icmp:
                    {
                        *(ushort*)(ptr + ipHeaderLength + 2) = 0;
                        *(ushort*)(ptr + ipHeaderLength + 2) = Checksum((ushort*)(ptr + ipHeaderLength), (uint)length - ipHeaderLength);
                    }
                    break;
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
