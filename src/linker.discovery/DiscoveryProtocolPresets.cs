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
                Remark = "Bonjour、DNS-SD、打印机及本地服务"
            },
            new DiscoveryProtocolInfo
            {
                Name = "SSDP",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 1900,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "UPnP、DLNA、路由器、NAS 及媒体设备"
            },
            new DiscoveryProtocolInfo
            {
                Name = "LLMNR",
                Address = IPAddress.Parse("224.0.0.252"),
                Port = 5355,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 255,
                Remark = "Windows 链路本地多播名称解析"
            },
            new DiscoveryProtocolInfo
            {
                Name = "NBNS",
                Address = IPAddress.Broadcast,
                Port = 137,
                Type = DiscoveryProtocolType.Broadcast,
                Ttl = 255,
                Remark = "NetBIOS 名称服务及旧版 Windows 发现"
            },
            new DiscoveryProtocolInfo
            {
                Name = "WS-Discovery",
                Address = IPAddress.Parse("239.255.255.250"),
                Port = 3702,
                Type = DiscoveryProtocolType.Multicast,
                Ttl = 4,
                Remark = "ONVIF、打印机及 Windows 网络发现"
            }
        ];
    }
}
