using System;

namespace linker.discovery
{
    public static class DiscoveryProtocolHandlerSelector
    {
        private static readonly IDiscoveryProtocolHandler Mdns = new DiscoveryProtocolHandlerMdns();
        private static readonly IDiscoveryProtocolHandler Llmnr = new DiscoveryProtocolHandlerLlmnr();
        private static readonly IDiscoveryProtocolHandler Nbns = new DiscoveryProtocolHandlerNbns();
        private static readonly IDiscoveryProtocolHandler Ssdp = new DiscoveryProtocolHandlerSsdp();
        private static readonly IDiscoveryProtocolHandler WsDiscovery = new DiscoveryProtocolHandlerWs();
        private static readonly IDiscoveryProtocolHandler Sadp = new DiscoveryProtocolHandlerSadp();
        private static readonly IDiscoveryProtocolHandler PayloadHash = new DiscoveryProtocolHandlerPayloadHash();

        public static IDiscoveryProtocolHandler Select(DiscoveryProtocolInfo protocol)
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

            if (protocol.Port == 37020 ||
                protocol.Name.Contains("sadp", StringComparison.OrdinalIgnoreCase) ||
                protocol.Name.Contains("hikvision", StringComparison.OrdinalIgnoreCase))
            {
                return Sadp;
            }

            return PayloadHash;
        }
    }

}
