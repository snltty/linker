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
                Remark = "苹果生态(Bonjour)、打印、智能家居"
            },
            new DiscoveryProtocolInfo
            {
                Name = "SSDP",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 1900,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "UPnP设备、媒体共享、智能家居"
            },
            new DiscoveryProtocolInfo
            {
                Name = "WS-Discovery",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 3702,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "企业级设备、专业安防(ONVIF)、打印机、Windows 设备发现"
            }
        ];
    }
}
