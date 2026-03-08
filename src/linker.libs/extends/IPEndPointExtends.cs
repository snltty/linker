using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace linker.libs.extends
{
    public static class IPEndPointExtends
    {
        public static bool IsCast(this IPAddress address)
        {
            Span<byte> bytes = stackalloc byte[4];
            address.TryWriteBytes(bytes, out _);
            return address.IsCast();
        }
        public static bool IsCast(this ReadOnlyMemory<byte> address)
        {
            return address.Span.IsCast();
        }
        public static bool IsCast(this ReadOnlySpan<byte> address)
        {
            return address.IsBroadcast() || address.IsMulticast();
        }

        public static bool IsBroadcast(this ReadOnlySpan<byte> address)
        {
            return address[3] == 255  || BinaryPrimitives.ReadUInt32BigEndian(address) == 0xFFFFFFFF;
        }
        public static bool IsMulticast(this ReadOnlySpan<byte> address)
        {
            return address[0] >= 0xE0 && address[0] <= 0xEF;
        }

        public static IPEndPoint MapToIPv4(this IPEndPoint ep)
        {
            if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6 && ep.Address.IsIPv4MappedToIPv6)
            {
                ep.Address = ep.Address.MapToIPv4();
            }
            return ep;
        }


        private static readonly HashSet<System.Net.IPNetwork> privateNetworks = new HashSet<System.Net.IPNetwork>
        {
            // IPv4 私有网络
            System.Net.IPNetwork.Parse("127.0.0.0/8"),    // 回环
            System.Net.IPNetwork.Parse("10.0.0.0/8"),      // 私有A类
            System.Net.IPNetwork.Parse("172.16.0.0/12"),   // 私有B类
            System.Net.IPNetwork.Parse("192.168.0.0/16"),  // 私有C类
            System.Net.IPNetwork.Parse("169.254.0.0/16"),  // 链路本地
            System.Net.IPNetwork.Parse("100.64.0.0/10"),   // CGNAT (可选)
        
            // IPv6 私有网络
            System.Net.IPNetwork.Parse("fc00::/7"),         // ULA
            System.Net.IPNetwork.Parse("fe80::/10"),        // 链路本地
            System.Net.IPNetwork.Parse("::1/128"),          // 回环
        };
        public static bool IsPrivateIP(this IPAddress ip)
        {
            return privateNetworks.Any(network => network.Contains(ip));
        }

    }
}
