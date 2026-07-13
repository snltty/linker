using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public static class DiscoveryProtocolKeyHelper
    {
        public static void AddPayloadHashKey(ICollection<string> keys, ulong hash)
        {
            AddDistinct(keys, "payload:" + hash.ToString("x16"));
        }

        public static void AddDistinct(ICollection<string> keys, string key)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        public static void AddLowerAsciiKey(ICollection<string> keys, string prefix, ReadOnlySpan<byte> value)
        {
            var builder = new StringBuilder(prefix.Length + value.Length);
            builder.Append(prefix);
            AppendLowerAscii(builder, value);
            AddDistinct(keys, builder.ToString());
        }

        public static void AddLowerUtf8Key(ICollection<string> keys, string prefix, ReadOnlySpan<byte> value)
        {
            value = TrimUtf8Whitespace(value);
            if (value.Length == 0)
            {
                return;
            }

            if (IsAscii(value))
            {
                AddLowerAsciiKey(keys, prefix, value);
                return;
            }

            string normalized = Encoding.UTF8.GetString(value).ToLowerInvariant();
            AddDistinct(keys, prefix + normalized);
        }

        private static void AppendLowerAscii(StringBuilder builder, ReadOnlySpan<byte> value)
        {
            foreach (byte item in value)
            {
                if (item is >= (byte)'A' and <= (byte)'Z')
                {
                    builder.Append((char)(item + 32));
                }
                else
                {
                    builder.Append((char)item);
                }
            }
        }

        private static bool IsAscii(ReadOnlySpan<byte> value)
        {
            foreach (byte item in value)
            {
                if (item > 0x7f)
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
