using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public static class DiscoveryProtocolXmlLite
    {
        public static bool TryGetElementText(ReadOnlySpan<byte> payload, string localName, out ReadOnlySpan<byte> value)
        {
            int offset = 0;
            while (offset < payload.Length)
            {
                int open = payload[offset..].IndexOf((byte)'<');
                if (open < 0)
                {
                    break;
                }

                open += offset;
                if (open + 1 >= payload.Length)
                {
                    break;
                }

                byte marker = payload[open + 1];
                if (marker == (byte)'/' || marker == (byte)'!' || marker == (byte)'?')
                {
                    offset = open + 1;
                    continue;
                }

                int nameStart = open + 1;
                int nameEnd = nameStart;
                while (nameEnd < payload.Length)
                {
                    byte current = payload[nameEnd];
                    if (current == (byte)'>' || current == (byte)'/' || current <= 0x20)
                    {
                        break;
                    }

                    nameEnd++;
                }

                ReadOnlySpan<byte> elementName = payload[nameStart..nameEnd];
                int colon = elementName.LastIndexOf((byte)':');
                if (colon >= 0)
                {
                    elementName = elementName[(colon + 1)..];
                }

                int tagEnd = payload[nameEnd..].IndexOf((byte)'>');
                if (tagEnd < 0)
                {
                    break;
                }

                tagEnd += nameEnd;
                if (!HttpLikeHeadersAsciiEquals(elementName, localName))
                {
                    offset = tagEnd + 1;
                    continue;
                }

                int close = payload[(tagEnd + 1)..].IndexOf((byte)'<');
                if (close < 0)
                {
                    break;
                }

                ReadOnlySpan<byte> rawValue = payload.Slice(tagEnd + 1, close);
                value = TrimUtf8Whitespace(rawValue);
                return true;
            }

            value = default;
            return false;
        }

        private static bool HttpLikeHeadersAsciiEquals(ReadOnlySpan<byte> left, string right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static ReadOnlySpan<byte> TrimUtf8Whitespace(ReadOnlySpan<byte> value)
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
    }

}
