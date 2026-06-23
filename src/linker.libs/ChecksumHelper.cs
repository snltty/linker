using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace linker.libs
{
    public sealed class ChecksumHelper
    {
        private const byte IpVersion = 4;

        private const byte ProtocolIcmp = 1;
        private const byte ProtocolIgmp = 2;
        private const byte ProtocolTcp = 6;
        private const byte ProtocolUdp = 17;
        private const uint StateValid = 1;
        private const uint IpOnlyStateMetadata = StateValid;
        private const int StateProtocolShift = 8;
        private const int StatePortOffsetShift = 16;
        private const int StateChecksumOffsetShift = 24;
        private const uint TcpStateMetadata =
            StateValid |
            ((uint)ProtocolTcp << StateProtocolShift) |
            (20u << StatePortOffsetShift) |
            (36u << StateChecksumOffsetShift);
        private const uint UdpStateMetadata =
            StateValid |
            ((uint)ProtocolUdp << StateProtocolShift) |
            (20u << StatePortOffsetShift) |
            (26u << StateChecksumOffsetShift);

        public readonly struct ChecksumState
        {
            internal readonly ReadOnlyMemory<byte> Packet;
            internal readonly ulong AddressPair;
            internal readonly uint PortPair;
            internal readonly uint Metadata;

            internal bool Valid
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return (Metadata & StateValid) != 0; }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ChecksumState(ReadOnlyMemory<byte> packet, bool valid, byte protocol, byte portOffset, byte checksumOffset, ulong addressPair, uint portPair)
            {
                Packet = packet;
                AddressPair = addressPair;
                PortPair = portPair;
                Metadata = valid
                    ? StateValid |
                      ((uint)protocol << StateProtocolShift) |
                      ((uint)portOffset << StatePortOffsetShift) |
                      ((uint)checksumOffset << StateChecksumOffsetShift)
                    : 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ChecksumState(ReadOnlyMemory<byte> packet, ulong addressPair, uint portPair, uint metadata)
            {
                Packet = packet;
                AddressPair = addressPair;
                PortPair = portPair;
                Metadata = metadata;
            }
        }

        public static byte[] CreateTcpOrUdpPacket(bool udp, int length = 60)
        {
            int transportHeaderLength = udp ? 8 : 20;
            if (length < 20 + transportHeaderLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            byte[] packet = new byte[length];
            packet[0] = 0x45;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), (ushort)length);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), 0x1234);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6, 2), 0x4000);
            packet[8] = 64;
            packet[9] = udp ? ProtocolUdp : ProtocolTcp;
            WriteUInt32BigEndian(packet, 12, 0x0A000001);
            WriteUInt32BigEndian(packet, 16, 0x0A000002);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(20, 2), 12345);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(22, 2), udp ? (ushort)53 : (ushort)443);

            if (udp)
            {
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(24, 2), (ushort)(length - 20));
                for (int i = 28; i < packet.Length; i++)
                    packet[i] = (byte)i;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(24, 4), 0x01020304);
                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(28, 4), 0x05060708);
                packet[32] = 0x50;
                packet[33] = 0x18;
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(34, 2), 65535);
                for (int i = 40; i < packet.Length; i++)
                    packet[i] = (byte)i;
            }

            RecalculateChecksum(packet);
            return packet;
        }
        public static void SetSourceIp(Span<byte> packet, uint sourceIp)
        {
            EnsureIpv4Packet(packet);
            WriteUInt32BigEndian(packet, 12, sourceIp);
        }
        public static void SetDestinationIp(Span<byte> packet, uint destinationIp)
        {
            EnsureIpv4Packet(packet);
            WriteUInt32BigEndian(packet, 16, destinationIp);
        }
        public static void SetSourcePort(Span<byte> packet, ushort sourcePort)
        {
            int portOffset = GetTransportHeaderOffsetForPortUpdate(packet);
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(portOffset, 2), sourcePort);
        }
        public static void SetDestinationPort(Span<byte> packet, ushort destinationPort)
        {
            int portOffset = GetTransportHeaderOffsetForPortUpdate(packet) + 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(portOffset, 2), destinationPort);
        }
        private static int GetTransportHeaderOffsetForPortUpdate(Span<byte> packet)
        {
            int ipHeaderLength = EnsureIpv4Packet(packet);
            if (IsNonFirstFragment(ref MemoryMarshal.GetReference(packet)))
                throw new InvalidOperationException("Cannot modify transport ports on a non-first IPv4 fragment.");

            byte protocol = packet[9];
            if (protocol != ProtocolTcp && protocol != ProtocolUdp)
                throw new InvalidOperationException("Only TCP and UDP packets have source and destination ports.");

            int neededLength = ipHeaderLength + (protocol == ProtocolTcp ? 20 : 8);
            if (packet.Length < neededLength)
                throw new ArgumentException("The packet is shorter than the transport header.", nameof(packet));

            return ipHeaderLength;
        }
        private static int EnsureIpv4Packet(Span<byte> packet)
        {
            if (packet.Length < 20 || (packet[0] >> 4) != IpVersion)
                throw new ArgumentException("The packet is not an IPv4 packet.", nameof(packet));

            int ipHeaderLength = (packet[0] & 0b1111) << 2;
            if (ipHeaderLength < 20 || packet.Length < ipHeaderLength)
                throw new ArgumentException("The packet has an invalid IPv4 header length.", nameof(packet));

            return ipHeaderLength;
        }
        private static void WriteUInt32BigEndian(Span<byte> packet, int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(offset, 4), value);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ChecksumState CaptureChecksumState(ReadOnlyMemory<byte> packet)
        {
            if ((uint)packet.Length >= 20)
            {
                ref byte packetRef = ref MemoryMarshal.GetReference(packet.Span);
                if ((packetRef >> 4) == IpVersion)
                {
                    int ipHeaderLength = (packetRef & 0b1111) << 2;
                    if (ipHeaderLength < 20 || (uint)packet.Length < (uint)ipHeaderLength)
                        return new ChecksumState(packet, false, 0, 0, 0, 0, 0);

                    if (IsNonFirstFragment(ref packetRef))
                        return new ChecksumState(packet, ReadUInt64Unaligned(ref packetRef, 12), 0, IpOnlyStateMetadata);

                    byte protocol = Unsafe.Add(ref packetRef, 9);
                    int packetLength = packet.Length - ipHeaderLength;
                    if (protocol == ProtocolTcp)
                    {
                        if (packetLength < 20)
                            return new ChecksumState(packet, false, 0, 0, 0, 0, 0);

                        return new ChecksumState(
                            packet,
                            ReadUInt64Unaligned(ref packetRef, 12),
                            ReadUInt32Unaligned(ref packetRef, ipHeaderLength),
                            ipHeaderLength == 20
                                ? TcpStateMetadata
                                : StateValid |
                                  ((uint)ProtocolTcp << StateProtocolShift) |
                                  ((uint)ipHeaderLength << StatePortOffsetShift) |
                                  ((uint)(ipHeaderLength + 16) << StateChecksumOffsetShift));
                    }

                    if (protocol == ProtocolUdp)
                    {
                        if (packetLength < 8)
                            return new ChecksumState(packet, false, 0, 0, 0, 0, 0);

                        return new ChecksumState(
                            packet,
                            ReadUInt64Unaligned(ref packetRef, 12),
                            ReadUInt32Unaligned(ref packetRef, ipHeaderLength),
                            ipHeaderLength == 20
                                ? UdpStateMetadata
                                : StateValid |
                                  ((uint)ProtocolUdp << StateProtocolShift) |
                                  ((uint)ipHeaderLength << StatePortOffsetShift) |
                                  ((uint)(ipHeaderLength + 6) << StateChecksumOffsetShift));
                    }

                    if ((protocol == ProtocolIcmp || protocol == ProtocolIgmp) && packetLength >= 4)
                    {
                        return new ChecksumState(
                            packet,
                            true,
                            protocol,
                            0,
                            (byte)(ipHeaderLength + 2),
                            ReadUInt64Unaligned(ref packetRef, 12),
                            0);
                    }
                }
            }

            return new ChecksumState(packet, false, 0, 0, 0, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool UpdateChecksum(scoped in ChecksumState state)
        {
            ReadOnlySpan<byte> packet = state.Packet.Span;
            if ((state.Metadata & StateValid) == 0)
            {
                RecalculateChecksum(packet);
                return false;
            }

            ref byte packetRef = ref MemoryMarshal.GetReference(packet);
            if (IsNonFirstFragment(ref packetRef))
            {
                int ipUpdateResult = TryUpdateIpHeaderChecksumFastPathRef(in state, ref packetRef);
                if (ipUpdateResult > 0)
                    return true;

                RecalculateChecksum(packet);
                return false;
            }

            int updateResult = TryUpdateChecksumFastPathRef(in state, ref packetRef);
            if (updateResult > 0)
                return true;

            RecalculateChecksum(packet);
            return false;
        }

        private static int TryUpdateIpHeaderChecksumFastPathRef(scoped in ChecksumState state, ref byte packetRef)
        {
            if (IsZero(ref packetRef, 10))
                return 0;

            ulong addressPair = ReadUInt64Unaligned(ref packetRef, 12);
            if (addressPair == state.AddressPair)
                return 1;

            WriteUInt16BigEndian(
                ref packetRef,
                10,
                IncrementalChecksumExpanded(
                    ReadUInt16BigEndian(ref packetRef, 10),
                    AddressPairComplementSum(state.AddressPair),
                    AddressPairSum(addressPair)));
            return 1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort IncrementalChecksumExpanded(ushort checksum, uint oldComplementSum, uint newSum)
        {
            uint sum = (uint)(~checksum & 0xFFFF) + oldComplementSum + newSum;
            sum = (sum & 0xFFFF) + (sum >> 16);
            sum = (sum & 0xFFFF) + (sum >> 16);
            return (ushort)~sum;
        }
        private static unsafe void RecalculateChecksum(ReadOnlySpan<byte> packet)
        {
            fixed (byte* ptr = packet)
            {
                RecalculateChecksum(ptr, packet.Length);
            }
        }
        private static unsafe void RecalculateChecksum(byte* ptr, int availableLength)
        {
            if (ptr == null || !HasLength(availableLength, 20) || (*ptr >> 4) != IpVersion)
                return;

            int ipHeaderLength = (*ptr & 0b1111) << 2;
            if (ipHeaderLength < 20 || !HasLength(availableLength, ipHeaderLength))
                return;

            int totalLength = ReadUInt16BigEndian(ptr + 2);
            if (totalLength < ipHeaderLength || !HasLength(availableLength, totalLength))
                return;

            WriteUInt16BigEndian(ptr + 10, 0);
            WriteUInt16BigEndian(ptr + 10, ComputeChecksum(ptr, (uint)ipHeaderLength));

            if (IsFragment(ptr))
                return;

            byte protocol = *(ptr + 9);
            byte* packetPtr = ptr + ipHeaderLength;
            int packetLength = totalLength - ipHeaderLength;

            switch (protocol)
            {
                case ProtocolTcp:
                    if (packetLength >= 20)
                        WriteTransportChecksum(packetPtr, packetLength, 16, PseudoHeaderSum(ptr, (uint)packetLength, protocol), false);
                    break;
                case ProtocolUdp:
                    if (packetLength >= 8)
                    {
                        int udpLength = ReadUInt16BigEndian(packetPtr + 4);
                        if (udpLength >= 8 && udpLength <= packetLength)
                            WriteTransportChecksum(packetPtr, udpLength, 6, PseudoHeaderSum(ptr, (uint)udpLength, protocol), true);
                    }
                    break;
                case ProtocolIcmp:
                case ProtocolIgmp:
                    if (packetLength >= 4)
                        WriteTransportChecksum(packetPtr, packetLength, 2, 0, false);
                    break;
            }
        }
        private static unsafe void WriteTransportChecksum(byte* packetPtr, int packetLength, int checksumOffset, ulong pseudoHeaderSum, bool mapZeroToAllOnes)
        {
            byte* checksumPtr = packetPtr + checksumOffset;
            WriteUInt16BigEndian(checksumPtr, 0);
            ushort checksum = ComputeChecksum(packetPtr, (uint)packetLength, pseudoHeaderSum);
            if (mapZeroToAllOnes && checksum == 0)
                checksum = 0xFFFF;
            WriteUInt16BigEndian(checksumPtr, checksum);
        }
        private static int TryUpdateChecksumFastPathRef(scoped in ChecksumState state, ref byte packetRef)
        {
            uint metadata = state.Metadata;
            int checksumOffset = (int)(metadata >> StateChecksumOffsetShift);
            if (HasZeroChecksum(ref packetRef, checksumOffset))
                return 0;

            if (metadata == TcpStateMetadata)
            {
                ulong currentAddressPair = ReadUInt64Unaligned(ref packetRef, 12);
                uint currentPortPair = ReadUInt32Unaligned(ref packetRef, 20);
                if (currentAddressPair == state.AddressPair && currentPortPair == state.PortPair)
                    return 1;

                UpdateTcpChangedChecksumRef(in state, ref packetRef, currentAddressPair, currentPortPair);
                return 1;
            }

            if (metadata == UdpStateMetadata)
            {
                ulong currentAddressPair = ReadUInt64Unaligned(ref packetRef, 12);
                uint currentPortPair = ReadUInt32Unaligned(ref packetRef, 20);
                if (currentAddressPair == state.AddressPair && currentPortPair == state.PortPair)
                    return 1;

                UpdateUdpChangedChecksumRef(in state, ref packetRef, currentAddressPair, currentPortPair);
                return 1;
            }

            int portOffset = (byte)(metadata >> StatePortOffsetShift);
            ulong addressPair = ReadUInt64Unaligned(ref packetRef, 12);
            uint portPair = portOffset == 0 ? 0 : ReadUInt32Unaligned(ref packetRef, portOffset);
            if (addressPair == state.AddressPair && portPair == state.PortPair)
                return 1;

            UpdateChangedChecksumRef(in state, ref packetRef, (byte)(metadata >> StateProtocolShift), checksumOffset, addressPair, portPair);
            return 1;
        }
        private static unsafe ushort ComputeChecksum(byte* addr, uint length, ulong pseudoHeaderSum = 0)
        {
            if (length == 0)
            {
                while ((pseudoHeaderSum >> 16) != 0)
                    pseudoHeaderSum = (pseudoHeaderSum & 0xFFFF) + (pseudoHeaderSum >> 16);
                return (ushort)~pseudoHeaderSum;
            }

            pseudoHeaderSum = ToLittleEndianSum(pseudoHeaderSum);
            ulong s0 = pseudoHeaderSum;
            ulong s1 = 0;
            ulong s2 = 0;
            ulong s3 = 0;
            ulong* ptr64 = (ulong*)addr;
            while (length > 31)
            {
                ulong value = ptr64[0];
                s0 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = ptr64[1];
                s1 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = ptr64[2];
                s2 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                value = ptr64[3];
                s3 += (value & 0xFFFF) + ((value >> 16) & 0xFFFF) + ((value >> 32) & 0xFFFF) + ((value >> 48) & 0xFFFF);
                ptr64 += 4;
                length -= 32;
            }

            pseudoHeaderSum = s0 + s1 + s2 + s3;
            ushort* ptr16 = (ushort*)ptr64;
            while (length > 1)
            {
                pseudoHeaderSum += *ptr16++;
                length -= 2;
            }

            if (length > 0)
                pseudoHeaderSum += *((byte*)ptr16);

            while ((pseudoHeaderSum >> 16) != 0)
                pseudoHeaderSum = (pseudoHeaderSum & 0xFFFF) + (pseudoHeaderSum >> 16);

            return BinaryPrimitives.ReverseEndianness((ushort)~pseudoHeaderSum);
        }
        private static unsafe ulong PseudoHeaderSum(byte* addr, uint length, byte protocol)
        {
            uint srcIp = BinaryPrimitives.ReverseEndianness(*(uint*)(addr + 12));
            uint dstIp = BinaryPrimitives.ReverseEndianness(*(uint*)(addr + 16));
            return (srcIp >> 16) + (srcIp & 0xFFFF) + (dstIp >> 16) + (dstIp & 0xFFFF) + protocol + length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasZeroChecksum(ref byte packetRef, int checksumOffset)
        {
            return IsZero(ref packetRef, 10) ||
                   (checksumOffset != 0 && IsZero(ref packetRef, checksumOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateTcpChangedChecksumRef(scoped in ChecksumState state, ref byte packetRef, ulong addressPair, uint portPair)
        {
            uint oldAddressComplementSum = 0;
            uint addressSum = 0;
            ulong oldAddressPair = state.AddressPair;
            uint oldSourceAddress = (uint)oldAddressPair;
            uint sourceAddress = (uint)addressPair;
            bool addressChanged = oldSourceAddress != sourceAddress;
            if (addressChanged)
            {
                oldAddressComplementSum += WordPairComplementSum(oldSourceAddress);
                addressSum += WordPairSum(sourceAddress);
            }

            uint oldDestinationAddress = (uint)(oldAddressPair >> 32);
            uint destinationAddress = (uint)(addressPair >> 32);
            if (oldDestinationAddress != destinationAddress)
            {
                addressChanged = true;
                oldAddressComplementSum += WordPairComplementSum(oldDestinationAddress);
                addressSum += WordPairSum(destinationAddress);
            }

            if (addressChanged)
                WriteUInt16BigEndian(ref packetRef, 10, IncrementalChecksumExpanded(ReadUInt16BigEndian(ref packetRef, 10), oldAddressComplementSum, addressSum));

            uint oldSum = oldAddressComplementSum;
            uint newSum = addressSum;
            uint oldPortPair = state.PortPair;
            ushort oldSourcePort = (ushort)oldPortPair;
            ushort sourcePort = (ushort)portPair;
            if (oldSourcePort != sourcePort)
            {
                oldSum += NetworkWordComplement(oldSourcePort);
                newSum += NetworkWord(sourcePort);
            }

            ushort oldDestinationPort = (ushort)(oldPortPair >> 16);
            ushort destinationPort = (ushort)(portPair >> 16);
            if (oldDestinationPort != destinationPort)
            {
                oldSum += NetworkWordComplement(oldDestinationPort);
                newSum += NetworkWord(destinationPort);
            }

            ushort transportChecksum = ReadUInt16BigEndianUnaligned(ref packetRef, 36);
            transportChecksum = IncrementalChecksumExpanded(transportChecksum, oldSum, newSum);
            WriteUInt16BigEndian(ref packetRef, 36, transportChecksum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateUdpChangedChecksumRef(scoped in ChecksumState state, ref byte packetRef, ulong addressPair, uint portPair)
        {
            uint oldAddressComplementSum = 0;
            uint addressSum = 0;
            ulong oldAddressPair = state.AddressPair;
            uint oldSourceAddress = (uint)oldAddressPair;
            uint sourceAddress = (uint)addressPair;
            bool addressChanged = oldSourceAddress != sourceAddress;
            if (addressChanged)
            {
                oldAddressComplementSum += WordPairComplementSum(oldSourceAddress);
                addressSum += WordPairSum(sourceAddress);
            }

            uint oldDestinationAddress = (uint)(oldAddressPair >> 32);
            uint destinationAddress = (uint)(addressPair >> 32);
            if (oldDestinationAddress != destinationAddress)
            {
                addressChanged = true;
                oldAddressComplementSum += WordPairComplementSum(oldDestinationAddress);
                addressSum += WordPairSum(destinationAddress);
            }

            if (addressChanged)
                WriteUInt16BigEndian(ref packetRef, 10, IncrementalChecksumExpanded(ReadUInt16BigEndian(ref packetRef, 10), oldAddressComplementSum, addressSum));

            uint oldSum = oldAddressComplementSum;
            uint newSum = addressSum;
            uint oldPortPair = state.PortPair;
            ushort oldSourcePort = (ushort)oldPortPair;
            ushort sourcePort = (ushort)portPair;
            if (oldSourcePort != sourcePort)
            {
                oldSum += NetworkWordComplement(oldSourcePort);
                newSum += NetworkWord(sourcePort);
            }

            ushort oldDestinationPort = (ushort)(oldPortPair >> 16);
            ushort destinationPort = (ushort)(portPair >> 16);
            if (oldDestinationPort != destinationPort)
            {
                oldSum += NetworkWordComplement(oldDestinationPort);
                newSum += NetworkWord(destinationPort);
            }

            ushort transportChecksum = ReadUInt16BigEndianUnaligned(ref packetRef, 26);
            transportChecksum = IncrementalChecksumExpanded(transportChecksum, oldSum, newSum);
            if (transportChecksum == 0)
                transportChecksum = 0xFFFF;
            WriteUInt16BigEndian(ref packetRef, 26, transportChecksum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateChangedChecksumRef(scoped in ChecksumState state, ref byte packetRef, byte protocol, int checksumOffset, ulong addressPair, uint portPair)
        {
            uint oldAddressSum = AddressPairSum(state.AddressPair);
            uint addressSum = AddressPairSum(addressPair);
            uint oldAddressComplementSum = AddressPairComplementSum(state.AddressPair);
            if (state.AddressPair != addressPair)
                WriteUInt16BigEndian(ref packetRef, 10, IncrementalChecksumExpanded(ReadUInt16BigEndian(ref packetRef, 10), oldAddressComplementSum, addressSum));

            bool hasPayloadChecksum = checksumOffset != 0;
            if (!hasPayloadChecksum)
                return;

            ushort newTransportChecksum = ReadUInt16BigEndian(ref packetRef, checksumOffset);
            bool hasPseudoHeader = protocol == ProtocolTcp || protocol == ProtocolUdp;
            if (!hasPseudoHeader)
                return;

            uint oldSum = oldAddressComplementSum + PortPairComplementSum(state.PortPair);
            uint newSum = addressSum + PortPairSum(portPair);
            newTransportChecksum = IncrementalChecksumExpanded(
                newTransportChecksum,
                oldSum,
                newSum);
            if (protocol == ProtocolUdp && newTransportChecksum == 0)
                newTransportChecksum = 0xFFFF;
            WriteUInt16BigEndian(ref packetRef, checksumOffset, newTransportChecksum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ToLittleEndianSum(ulong sum)
        {
            while ((sum >> 16) != 0)
                sum = (sum & 0xFFFF) + (sum >> 16);

            return BinaryPrimitives.ReverseEndianness((ushort)sum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddressPairSum(ulong addressPair)
        {
            return (uint)BinaryPrimitives.ReverseEndianness((ushort)addressPair) +
                   BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 16)) +
                   BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 32)) +
                   BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 48));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint WordPairSum(uint value)
        {
            return (uint)BinaryPrimitives.ReverseEndianness((ushort)value) +
                   BinaryPrimitives.ReverseEndianness((ushort)(value >> 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint WordPairComplementSum(uint value)
        {
            return (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)value)) +
                   (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)(value >> 16)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AddressPairComplementSum(ulong addressPair)
        {
            return (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)addressPair)) +
                   (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 16))) +
                   (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 32))) +
                   (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)(addressPair >> 48)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint NetworkWord(ushort value)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint NetworkWordComplement(ushort value)
        {
            return 0xFFFFu ^ BinaryPrimitives.ReverseEndianness(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PortPairSum(uint portPair)
        {
            return (uint)BinaryPrimitives.ReverseEndianness((ushort)portPair) +
                   BinaryPrimitives.ReverseEndianness((ushort)(portPair >> 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PortPairComplementSum(uint portPair)
        {
            return (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)portPair)) +
                   (0xFFFFu ^ BinaryPrimitives.ReverseEndianness((ushort)(portPair >> 16)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasLength(int availableLength, int needed)
        {
            return availableLength < 0 || (uint)needed <= (uint)availableLength;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool IsFragment(byte* ptr)
        {
            return ((*(ptr + 6) & 0x3F) | *(ptr + 7)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonFirstFragment(ref byte packetRef)
        {
            return ((Unsafe.Add(ref packetRef, 6) & 0x1F) | Unsafe.Add(ref packetRef, 7)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ushort ReadUInt16BigEndian(byte* ptr)
        {
            return (ushort)((*ptr << 8) | *(ptr + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadUInt16BigEndian(ref byte packetRef, int offset)
        {
            return (ushort)((Unsafe.Add(ref packetRef, offset) << 8) | Unsafe.Add(ref packetRef, offset + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadUInt16BigEndianUnaligned(ref byte packetRef, int offset)
        {
            return BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref packetRef, offset)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUInt32Unaligned(ref byte packetRef, int offset)
        {
            return Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref packetRef, offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadUInt64Unaligned(ref byte packetRef, int offset)
        {
            return Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref packetRef, offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(ref byte packetRef, int offset)
        {
            return (Unsafe.Add(ref packetRef, offset) | Unsafe.Add(ref packetRef, offset + 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteUInt16BigEndian(byte* ptr, ushort value)
        {
            *ptr = (byte)(value >> 8);
            *(ptr + 1) = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUInt16BigEndian(ref byte packetRef, int offset, ushort value)
        {
            Unsafe.Add(ref packetRef, offset) = (byte)(value >> 8);
            Unsafe.Add(ref packetRef, offset + 1) = (byte)value;
        }

    }
}
