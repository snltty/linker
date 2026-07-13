using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolHandlerSadp : IDiscoveryProtocolHandler
    {
        public bool ReceiveLanResponsesOnForwardSockets => true;

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

        public ReadOnlyMemory<byte> RewritePayload(
            DiscoveryProtocolRewriteContext context,
            ReadOnlyMemory<byte> payload,
            out bool rewritten)
        {
            byte[]? next = DiscoveryProtocolPayloadRewriteHelper.RewriteXmlIPv4ElementTexts(
                context,
                payload,
                "IPv4Address",
                "IPv4Gateway");
            rewritten = next is not null;
            return next ?? payload;
        }
    }
}
