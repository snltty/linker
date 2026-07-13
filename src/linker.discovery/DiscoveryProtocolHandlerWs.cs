using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolHandlerWs : IDiscoveryProtocolHandler
    {
        public bool ReceiveLanResponsesOnForwardSockets => true;

        public int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            if (!DiscoveryProtocolXmlLite.TryGetElementText(payload, "MessageID", out ReadOnlySpan<byte> value))
            {
                return 0;
            }

            int before = keys.Count;
            DiscoveryProtocolKeyHelper.AddLowerUtf8Key(keys, "wsd:", value);
            return keys.Count - before;
        }

        public int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys)
        {
            if (!DiscoveryProtocolXmlLite.TryGetElementText(payload, "RelatesTo", out ReadOnlySpan<byte> value))
            {
                return 0;
            }

            int before = keys.Count;
            DiscoveryProtocolKeyHelper.AddLowerUtf8Key(keys, "wsd:", value);
            return keys.Count - before;
        }

        public ReadOnlyMemory<byte> RewritePayload(
            DiscoveryProtocolRewriteContext context,
            ReadOnlyMemory<byte> payload,
            out bool rewritten)
        {
            rewritten = false;
            if (payload.Length == 0)
            {
                return payload;
            }

            byte[]? next = DiscoveryProtocolPayloadRewriteHelper.RewriteXmlElementUrlHosts(context, payload, "XAddrs");
            DiscoveryProtocolPayloadRewriteHelper.ReportXmlElementRewrite(
                context,
                "XAddrs",
                payload.Span,
                next ?? payload.Span);

            if (next is null)
            {
                return payload;
            }

            rewritten = true;
            return next;
        }
    }

}
