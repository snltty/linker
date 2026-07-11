using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolMatcherLlmnr : IDiscoveryProtocolMatcher
    {
        public int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            return DiscoveryProtocolMatcherDnsLike.GetQueryKeys(payload, keys, "llmnr");
        }

        public int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            return DiscoveryProtocolMatcherDnsLike.GetResponseKeys(payload, keys, "llmnr");
        }
    }
}
