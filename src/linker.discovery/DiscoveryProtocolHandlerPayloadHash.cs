using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolHandlerPayloadHash : IDiscoveryProtocolHandler
    {
        public bool AllowRecentResponseFallback => true;

        public int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            DiscoveryProtocolKeyHelper.AddPayloadHashKey(keys, DiscoveryProtocolPacketHash.Compute(payload));
            return 1;
        }

        public int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            DiscoveryProtocolKeyHelper.AddPayloadHashKey(keys, DiscoveryProtocolPacketHash.Compute(payload));
            return 1;
        }
    }
}
