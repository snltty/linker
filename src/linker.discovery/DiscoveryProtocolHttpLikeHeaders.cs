using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public static class DiscoveryProtocolHttpLikeHeaders
    {
        public static bool TryGetHeader(ReadOnlySpan<byte> payload, string headerName, out ReadOnlySpan<byte> value)
        {
            int offset = 0;
            while (offset < payload.Length)
            {
                int lineEnd = IndexOfLineEnd(payload[offset..]);
                ReadOnlySpan<byte> line = lineEnd >= 0 ? payload.Slice(offset, lineEnd) : payload[offset..];
                offset += lineEnd >= 0 ? lineEnd + GetLineBreakLength(payload[(offset + lineEnd)..]) : payload.Length - offset;

                int colon = line.IndexOf((byte)':');
                if (colon <= 0)
                {
                    continue;
                }

                ReadOnlySpan<byte> name = TrimAscii(line[..colon]);
                if (!AsciiEqualsIgnoreCase(name, headerName))
                {
                    continue;
                }

                ReadOnlySpan<byte> rawValue = TrimAscii(line[(colon + 1)..]);
                value = rawValue;
                return true;
            }

            value = default;
            return false;
        }

        private static int IndexOfLineEnd(ReadOnlySpan<byte> value)
        {
            int lf = value.IndexOf((byte)'\n');
            if (lf < 0)
            {
                return -1;
            }

            return lf > 0 && value[lf - 1] == '\r' ? lf - 1 : lf;
        }

        private static int GetLineBreakLength(ReadOnlySpan<byte> value)
        {
            if (value.Length >= 2 && value[0] == '\r' && value[1] == '\n')
            {
                return 2;
            }

            return value.Length >= 1 && value[0] == '\n' ? 1 : 0;
        }

        public static ReadOnlySpan<byte> TrimAscii(ReadOnlySpan<byte> value)
        {
            int start = 0;
            int end = value.Length - 1;

            while (start <= end && value[start] <= 0x20)
            {
                start++;
            }

            while (end >= start && value[end] <= 0x20)
            {
                end--;
            }

            return value.Slice(start, end - start + 1);
        }

        public static bool AsciiEqualsIgnoreCase(ReadOnlySpan<byte> left, string right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                byte a = left[i];
                char b = right[i];
                if (a is >= (byte)'A' and <= (byte)'Z')
                {
                    a = (byte)(a + 32);
                }

                if (b is >= 'A' and <= 'Z')
                {
                    b = (char)(b + 32);
                }

                if (a != b)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
