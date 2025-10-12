using System;
using System.Buffers.Binary;
using System.Net.Sockets;
namespace linker.libs
{
    public sealed class ChecksumHelper
    {

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
            ulong* ptr64 = (ulong*)addr;
            while (length > 31)
            {
                ulong value = BinaryPrimitives.ReverseEndianness(*ptr64++);
                pseudoHeaderSum += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = BinaryPrimitives.ReverseEndianness(*ptr64++);
                pseudoHeaderSum += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = BinaryPrimitives.ReverseEndianness(*ptr64++);
                pseudoHeaderSum += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = BinaryPrimitives.ReverseEndianness(*ptr64++);
                pseudoHeaderSum += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                length -= 32;
                pseudoHeaderSum = (pseudoHeaderSum & 0xffff) + (pseudoHeaderSum >> 16);
            }

            ushort* ptr16 = (ushort*)ptr64;
            while (length > 1)
            {
                pseudoHeaderSum += BinaryPrimitives.ReverseEndianness(*ptr16++);
                length -= 2;
                pseudoHeaderSum = (pseudoHeaderSum & 0xffff) + (pseudoHeaderSum >> 16);
            }
            if (length > 0) pseudoHeaderSum += (ushort)((*ptr16) << 8);
            while ((pseudoHeaderSum >> 16) != 0) pseudoHeaderSum = (pseudoHeaderSum & 0xffff) + (pseudoHeaderSum >> 16);
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
            uint srcIp = BinaryPrimitives.ReverseEndianness(*(uint*)(addr + 12));
            uint dstIp = BinaryPrimitives.ReverseEndianness(*(uint*)(addr + 16));
            return (srcIp >> 16) + (srcIp & 0xFFFF) + (dstIp >> 16) + (dstIp & 0xFFFF) + *(addr + 9) + length;
        }
    }
}
