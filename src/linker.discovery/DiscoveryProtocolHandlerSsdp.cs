using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolHandlerSsdp : IDiscoveryProtocolHandler
    {
        public bool ReceiveLanResponsesOnForwardSockets => true;

        public int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            if (!DiscoveryProtocolHttpLikeHeaders.TryGetHeader(payload, "ST", out ReadOnlySpan<byte> value))
            {
                return 0;
            }

            int before = keys.Count;
            AddSsdpKeys(value, keys, query: true);
            return keys.Count - before;
        }

        public int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            if (!IsHttpResponse(payload))
            {
                return 0;
            }

            if (!DiscoveryProtocolHttpLikeHeaders.TryGetHeader(payload, "ST", out ReadOnlySpan<byte> value))
            {
                return 0;
            }

            int before = keys.Count;
            AddSsdpKeys(value, keys, query: false);
            return keys.Count - before;
        }

        public ReadOnlyMemory<byte> RewritePayload(
            DiscoveryProtocolRewriteContext context,
            ReadOnlyMemory<byte> payload,
            out bool rewritten)
        {
            byte[]? next = DiscoveryProtocolPayloadRewriteHelper.RewriteHttpHeaderUrlHosts(context, payload, "LOCATION");
            rewritten = next is not null;
            return next ?? payload;
        }

        private static void AddSsdpKeys(ReadOnlySpan<byte> value, ICollection<string> keys, bool query)
        {
            value = DiscoveryProtocolHttpLikeHeaders.TrimAscii(value);
            if (value.Length == 0)
            {
                return;
            }

            DiscoveryProtocolKeyHelper.AddLowerAsciiKey(keys, "ssdp:", value);
            if (!query || DiscoveryProtocolHttpLikeHeaders.AsciiEqualsIgnoreCase(value, "ssdp:all"))
            {
                DiscoveryProtocolKeyHelper.AddDistinct(keys, "ssdp:*");
            }
        }

        private static bool IsHttpResponse(ReadOnlySpan<byte> payload)
        {
            ReadOnlySpan<byte> line = payload;
            int lf = payload.IndexOf((byte)'\n');
            if (lf >= 0)
            {
                line = payload[..lf];
            }

            line = DiscoveryProtocolHttpLikeHeaders.TrimAscii(line);
            return StartsWithAsciiIgnoreCase(line, "HTTP/");
        }

        private static bool StartsWithAsciiIgnoreCase(ReadOnlySpan<byte> value, string prefix)
        {
            if (value.Length < prefix.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                byte left = value[i];
                char right = prefix[i];
                if (left is >= (byte)'A' and <= (byte)'Z')
                {
                    left = (byte)(left + 32);
                }

                if (right is >= 'A' and <= 'Z')
                {
                    right = (char)(right + 32);
                }

                if (left != right)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
