using System.Collections.Generic;
using System.Net;

namespace linker.discovery;

public static class DiscoveryProtocolPresets
{
    public static List<DiscoveryProtocolInfo> CreateDefault()
    {
        return
        [
            new DiscoveryProtocolInfo
            {
                Name = "mDNS",
                Address = IPAddress.Parse("224.0.0.251"),
                Port = 5353,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 255,
                Remark = "Bonjour, DNS-SD, printers, and local services"
            },
            new DiscoveryProtocolInfo
            {
                Name = "SSDP",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 1900,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "UPnP, DLNA, routers, NAS, and media devices"
            },
            new DiscoveryProtocolInfo
            {
                Name = "LLMNR",
                Address = IPAddress.Parse("224.0.0.252"),
                Port = 5355,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 255,
                Remark = "Windows link-local multicast name resolution"
            },
            new DiscoveryProtocolInfo
            {
                Name = "NBNS",
                Address = IPAddress.Broadcast,
                Port = 137,
                Type = DiscoveryProtocolType.Broadcast,
                Ttl = 255,
                Remark = "NetBIOS name service and legacy Windows discovery"
            },
            new DiscoveryProtocolInfo
            {
                Name = "WS-Discovery",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 3702,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "ONVIF, printers, and Windows network discovery"
            },
            new DiscoveryProtocolInfo
            {
                Name = "Hikvision-SADP",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 37020,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "Hikvision SADP transparent discovery relay"
            }
        ];
    }
}
