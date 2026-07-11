using System;

namespace linker.discovery
{
    public static class DiscoveryProtocolMatcherSelector
    {
        private static readonly IDiscoveryProtocolMatcher Mdns = new DiscoveryProtocolMatcherMdns();
        private static readonly IDiscoveryProtocolMatcher Llmnr = new DiscoveryProtocolMatcherLlmnr();
        private static readonly IDiscoveryProtocolMatcher Nbns = new DiscoveryProtocolMatcherNbns();
        private static readonly IDiscoveryProtocolMatcher Ssdp = new DiscoveryProtocolMatcherSsdp();
        private static readonly IDiscoveryProtocolMatcher WsDiscovery = new DiscoveryProtocolMatcherWs();
        private static readonly IDiscoveryProtocolMatcher PayloadHash = new DiscoveryProtocolMatcherPayloadHash();

        public static IDiscoveryProtocolMatcher Select(DiscoveryProtocolInfo protocol)
        {
            if (protocol.Port == 5353 ||
                protocol.Name.Contains("mdns", StringComparison.OrdinalIgnoreCase))
            {
                return Mdns;
            }

            if (protocol.Port == 5355 ||
                protocol.Name.Contains("llmnr", StringComparison.OrdinalIgnoreCase))
            {
                return Llmnr;
            }

            if (protocol.Port == 137 ||
                protocol.Name.Contains("nbns", StringComparison.OrdinalIgnoreCase) ||
                protocol.Name.Contains("netbios", StringComparison.OrdinalIgnoreCase))
            {
                return Nbns;
            }

            if (protocol.Port == 1900 ||
                protocol.Name.Contains("ssdp", StringComparison.OrdinalIgnoreCase) ||
                protocol.Name.Contains("upnp", StringComparison.OrdinalIgnoreCase))
            {
                return Ssdp;
            }

            if (protocol.Port == 3702 ||
                protocol.Name.Contains("ws-discovery", StringComparison.OrdinalIgnoreCase) ||
                protocol.Name.Contains("onvif", StringComparison.OrdinalIgnoreCase))
            {
                return WsDiscovery;
            }

            return PayloadHash;
        }
    }

}
