using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolMatcherSsdp : IDiscoveryProtocolMatcher
    {
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
