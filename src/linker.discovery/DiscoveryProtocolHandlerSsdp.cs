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
            byte[]? next = DiscoveryProtocolPayloadRewriteHelper.RewriteUrlHosts(context, payload);
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
    }
}
