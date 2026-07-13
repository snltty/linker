using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public static class DiscoveryProtocolDnsPacket
    {
        public static ushort ReadUInt16(ReadOnlySpan<byte> payload, int offset)
        {
            return (ushort)((payload[offset] << 8) | payload[offset + 1]);
        }

        public static bool TryReadQuestion(
            ReadOnlySpan<byte> payload,
            ref int offset,
            out string name,
            out ushort type,
            out ushort dnsClass)
        {
            if (!TryReadName(payload, offset, out name, out int nextOffset) ||
                nextOffset + 4 > payload.Length)
            {
                type = 0;
                dnsClass = 0;
                return false;
            }

            type = ReadUInt16(payload, nextOffset);
            dnsClass = ReadUInt16(payload, nextOffset + 2);
            offset = nextOffset + 4;
            return true;
        }

        public static bool TryReadResourceRecordHeader(
            ReadOnlySpan<byte> payload,
            ref int offset,
            out string name,
            out ushort type,
            out ushort dnsClass,
            out ushort dataLength)
        {
            if (!TryReadName(payload, offset, out name, out int nextOffset) ||
                nextOffset + 10 > payload.Length)
            {
                type = 0;
                dnsClass = 0;
                dataLength = 0;
                return false;
            }

            type = ReadUInt16(payload, nextOffset);
            dnsClass = ReadUInt16(payload, nextOffset + 2);
            dataLength = ReadUInt16(payload, nextOffset + 8);
            offset = nextOffset + 10;
            return true;
        }

        private static bool TryReadName(ReadOnlySpan<byte> payload, int offset, out string name, out int nextOffset)
        {
            var builder = new StringBuilder(64);
            int current = offset;
            int jumps = 0;
            bool jumped = false;
            nextOffset = -1;

            while (current < payload.Length)
            {
                byte length = payload[current];
                if (length == 0)
                {
                    if (!jumped)
                    {
                        nextOffset = current + 1;
                    }

                    name = builder.ToString();
                    return true;
                }

                if ((length & 0xc0) == 0xc0)
                {
                    if (current + 1 >= payload.Length)
                    {
                        break;
                    }

                    int pointer = ((length & 0x3f) << 8) | payload[current + 1];
                    if (pointer >= payload.Length)
                    {
                        break;
                    }

                    if (!jumped)
                    {
                        nextOffset = current + 2;
                    }

                    current = pointer;
                    jumped = true;
                    jumps++;
                    if (jumps > 16)
                    {
                        break;
                    }

                    continue;
                }

                if ((length & 0xc0) != 0)
                {
                    break;
                }

                current++;
                if (current + length > payload.Length)
                {
                    break;
                }

                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                AppendAsciiLower(builder, payload.Slice(current, length));
                current += length;
            }

            name = string.Empty;
            nextOffset = offset;
            return false;
        }

        private static void AppendAsciiLower(StringBuilder builder, ReadOnlySpan<byte> value)
        {
            foreach (byte item in value)
            {
                if (item is >= (byte)'A' and <= (byte)'Z')
                {
                    builder.Append((char)(item + 32));
                }
                else if (item is >= 0x20 and <= 0x7e)
                {
                    builder.Append((char)item);
                }
                else
                {
                    builder.Append('\\');
                    builder.Append(item.ToString("d3"));
                }
            }
        }
    }
}
