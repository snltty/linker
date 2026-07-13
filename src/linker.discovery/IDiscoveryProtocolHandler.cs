using System;
using System.Collections.Generic;

namespace linker.discovery;

public interface IDiscoveryProtocolHandler
{
    bool ReceiveLanResponsesOnForwardSockets => false;

    bool AllowRecentResponseFallback => false;

    int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys);

    int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys);

    ReadOnlyMemory<byte> RewritePayload(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> payload,
        out bool rewritten)
    {
        rewritten = false;
        return payload;
    }
}
