using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace linker.libs
{
    public sealed class ChecksumHelper
    {
        public readonly struct ChecksumState
        {
            public readonly ulong Addr;
            public readonly uint Port;
            public readonly int ChecksumOffset;

            public ChecksumState(ulong addr, uint port, int checksumOffset)
            {
                Addr = addr;
                Port = port;
                ChecksumOffset = checksumOffset;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChecksumState CaptureChecksumState(scoped in ReadOnlyMemory<byte> memory)
        {
            return CaptureChecksumState(memory.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChecksumState CaptureChecksumState(scoped in ReadOnlySpan<byte> span)
        {
            ref byte packetRef = ref MemoryMarshal.GetReference(span);
            return CaptureChecksumState(ref packetRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChecksumState CaptureChecksumState(ref byte packetRef)
        {
            int hlen = (packetRef & 0x0F) << 2;
            byte protocol = Unsafe.Add(ref packetRef, 9);
            ulong addr = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref packetRef, 12));

            if (((Unsafe.Add(ref packetRef, 6) & 0x1F) | Unsafe.Add(ref packetRef, 7)) == 0)
            {
                if (protocol == 6)
                {
                    return new ChecksumState(addr, Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref packetRef, hlen)), hlen + 16);
                }
                else if (protocol == 17)
                {
                    return new ChecksumState(addr, Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref packetRef, hlen)), hlen + 6);
                }
            }

            return new ChecksumState(addr, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChecksumState CaptureChecksumState(ref byte packetRef, uint oldPort)
        {
            ulong addr = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref packetRef, 12));
            uint port = 0;
            if (oldPort > 0)
            {
                int hlen = (packetRef & 0x0F) << 2;
                port = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref packetRef, hlen));
            }
            return new ChecksumState(addr, port, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool UpdateChecksum(scoped in ChecksumState state, scoped in ReadOnlyMemory<byte> memory)
        {
            return UpdateChecksum(state, memory.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool UpdateChecksum(scoped in ChecksumState state, scoped in ReadOnlySpan<byte> span)
        {
            ref byte packetRef = ref MemoryMarshal.GetReference(span);
            ChecksumState newState = CaptureChecksumState(ref packetRef, state.Port);

            bool hasIp = state.Addr != newState.Addr;
            bool hasTransport = state.Port > 0 && (hasIp || state.Port != newState.Port);
            if (hasIp == false && hasTransport == false)
            {
                return false;
            }

            if (IsZero(ref packetRef, 10) || (hasTransport && IsZero(ref packetRef, state.ChecksumOffset)))
            {
                RecalculateChecksum(span);
                return true;
            }

            uint sumIp = 0;
            if (hasIp)
            {
                uint sum = (uint)(~ReadRawWord(ref packetRef, 10) & 0xFFFF);

                uint oldAddressComplementSum = AddressPairRawComplementSum(state.Addr);
                uint newAddressSum = AddressPairRawSum(newState.Addr);
                sum += oldAddressComplementSum + newAddressSum;
                sumIp = oldAddressComplementSum + newAddressSum;

                WriteRawWord(ref packetRef, 10, Fold(sum));
            }

            if (hasTransport)
            {
                uint sum = (uint)(~ReadRawWord(ref packetRef, state.ChecksumOffset) & 0xFFFF);

                uint oldPortComplementSum = PortPairRawComplementSum(state.Port);
                uint newPortSum = PortPairRawSum(newState.Port);
                sum += sumIp + oldPortComplementSum + newPortSum;

                WriteRawWord(ref packetRef, state.ChecksumOffset, Fold(sum));
            }
            return true;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Fold(uint sum)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
            sum = (sum & 0xFFFF) + (sum >> 16);
            return (ushort)~sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddressPairRawSum(ulong value)
        {
            return (uint)(ushort)value +
                   (ushort)(value >> 16) +
                   (ushort)(value >> 32) +
                   (ushort)(value >> 48);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddressPairRawComplementSum(ulong value)
        {
            return 0x3FFFCu - AddressPairRawSum(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PortPairRawSum(uint value)
        {
            return (uint)(ushort)value +
                   (ushort)(value >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PortPairRawComplementSum(uint value)
        {
            return 0x1FFFEu - PortPairRawSum(value);
        }


        private static unsafe void RecalculateChecksum(ReadOnlySpan<byte> packet)
        {
            fixed (byte* ptr = packet)
            {
                RecalculateChecksum(ptr, packet.Length);
            }
        }
        private static unsafe void RecalculateChecksum(byte* ptr, int length)
        {
            int hlen = (*ptr & 0x0F) * 4;
            int totalLength = ReadWord((ushort*)(ptr + 2));
            if ((uint)totalLength > (uint)length)
            {
                return;
            }

            WriteWord((ushort*)(ptr + 10), 0);
            WriteWord((ushort*)(ptr + 10), ComputeChecksum(ptr, (uint)hlen));

            byte protocol = ptr[9];
            byte* transportPtr = ptr + hlen;
            if (protocol == 6)
            {
                int tcpLength = totalLength - hlen;
                WriteTransportChecksum(transportPtr, tcpLength, 16, PseudoHeaderSum(ptr, (uint)tcpLength, protocol));
            }
            else if (protocol == 17)
            {
                int udpLength = ReadWord((ushort*)(transportPtr + 4));
                WriteTransportChecksum(transportPtr, udpLength, 6, PseudoHeaderSum(ptr, (uint)udpLength, protocol));
            }
        }
        private static unsafe ushort ComputeChecksum(byte* ptr, uint length, ulong pseudoHeaderSum = 0)
        {
            pseudoHeaderSum = ToLittleEndianSum(pseudoHeaderSum);
            ulong s0 = pseudoHeaderSum;
            ulong s1 = 0;
            ulong s2 = 0;
            ulong s3 = 0;

            while (length > 31)
            {
                ulong value = Unsafe.ReadUnaligned<ulong>(ptr);
                s0 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                ptr += 8;

                value = Unsafe.ReadUnaligned<ulong>(ptr);
                s1 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                ptr += 8;

                value = Unsafe.ReadUnaligned<ulong>(ptr);
                s2 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                ptr += 8;

                value = Unsafe.ReadUnaligned<ulong>(ptr);
                s3 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                ptr += 8;
                length -= 32;
            }

            pseudoHeaderSum = s0 + s1 + s2 + s3;
            while (length > 1)
            {
                pseudoHeaderSum += Unsafe.ReadUnaligned<ushort>(ptr);
                ptr += 2;
                length -= 2;
            }

            if (length > 0)
            {
                pseudoHeaderSum += *ptr;
            }

            while ((pseudoHeaderSum >> 16) != 0)
            {
                pseudoHeaderSum = (pseudoHeaderSum & 0xFFFF) + (pseudoHeaderSum >> 16);
            }

            return BinaryPrimitives.ReverseEndianness((ushort)~pseudoHeaderSum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong PseudoHeaderSum(byte* ptr, uint length, byte protocol)
        {
            return AddressPairSum(Unsafe.ReadUnaligned<ulong>(ptr + 12)) + protocol + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddressPairSum(ulong value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return (uint)(ushort)value +
                   (ushort)(value >> 16) +
                   (ushort)(value >> 32) +
                   (ushort)(value >> 48);
        }





        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteTransportChecksum(byte* ptr, int length, int offset, ulong pseudoHeaderSum)
        {
            WriteWord((ushort*)(ptr + offset), 0);
            WriteWord((ushort*)(ptr + offset), ComputeChecksum(ptr, (uint)length, pseudoHeaderSum));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ToLittleEndianSum(ulong sum)
        {
            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            return BinaryPrimitives.ReverseEndianness((ushort)sum);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ushort ReadWord(ushort* ptr) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ptr));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteWord(ushort* ptr, ushort value) => *ptr = BinaryPrimitives.ReverseEndianness(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadRawWord(ref byte packetRef, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref packetRef, offset));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteRawWord(ref byte packetRef, int offset, ushort value) => Unsafe.WriteUnaligned(ref Unsafe.Add(ref packetRef, offset), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(ref byte packetRef, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref packetRef, offset)) == 0;


        public static unsafe bool CreateIcmpHostUnreachablePacket(Span<byte> packet)
        {
            if (packet.Length < 20)
            {
                return false;
            }

            int ipHeaderLength = (packet[0] & 0x0F) << 2;
            if (ipHeaderLength < 20 || ipHeaderLength > packet.Length)
            {
                return false;
            }

            if (packet[9] != 1 || packet[ipHeaderLength] != 8)
            {
                return false;
            }

            int quotedLength = ipHeaderLength + 8;
            int icmpOffset = 20;
            int icmpLength = 8 + quotedLength;
            int totalLength = icmpOffset + icmpLength;
            if (totalLength > packet.Length)
            {
                return false;
            }

            byte tos = packet[1];
            uint sourceAddress = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(12, 4));
            uint destinationAddress = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(16, 4));

            packet.Slice(0, quotedLength).CopyTo(packet.Slice(icmpOffset + 8, quotedLength));

            packet[0] = 0x45;
            packet[1] = tos;
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(2, 2), (ushort)totalLength);
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(4, 2), 0);
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(6, 2), 0);
            packet[8] = 64;
            packet[9] = 1;

            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(12, 4), destinationAddress);
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(16, 4), sourceAddress);

            packet[icmpOffset] = 3;
            packet[icmpOffset + 1] = 1;
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(icmpOffset + 2, 2), 0);
            packet[icmpOffset + 4] = 0;
            packet[icmpOffset + 5] = 0;
            packet[icmpOffset + 6] = 0;
            packet[icmpOffset + 7] = 0;

            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(10, 2), 0);

            fixed (byte* ptr = packet)
            {
                WriteWord((ushort*)(ptr + 10), ComputeChecksum(ptr, 20));
                WriteWord((ushort*)(ptr + icmpOffset + 2), ComputeChecksum(ptr + icmpOffset, (uint)icmpLength));
            }

            return true;
        }
    }

}
