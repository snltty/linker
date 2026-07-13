using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolHandlerNbns : IDiscoveryProtocolHandler
    {
        public bool ReceiveLanResponsesOnForwardSockets => true;

        public int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            return DiscoveryProtocolHandlerDnsLike.GetQueryKeys(payload, keys, "nbns");
        }

        public int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            return DiscoveryProtocolHandlerDnsLike.GetResponseKeys(payload, keys, "nbns");
        }

        public ReadOnlyMemory<byte> RewritePayload(
            DiscoveryProtocolRewriteContext context,
            ReadOnlyMemory<byte> payload,
            out bool rewritten)
        {
            byte[]? next = DiscoveryProtocolPayloadRewriteHelper.RewriteDnsLikeAddresses(context, payload, rewriteNbRecords: true);
            rewritten = next is not null;
            return next ?? payload;
        }
    }
}
