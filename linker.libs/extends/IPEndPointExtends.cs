using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;

namespace linker.libs.extends
{
    public static class IPEndPointExtends
    {
        public static Memory<byte> ipv6Loopback = IPAddress.IPv6Loopback.GetAddressBytes();
        public static Memory<byte> ipv6Multicast = IPAddress.Parse("ff00::").GetAddressBytes();
        public static Memory<byte> ipv6Local = IPAddress.Parse("fe80::").GetAddressBytes();
        public static byte[] anyIpArray = IPAddress.Any.GetAddressBytes();
        public static byte[] anyIpv6Array = IPAddress.IPv6Any.GetAddressBytes();

        public static bool IsLan(this IPEndPoint endPoint)
        {
            if (endPoint == null) return false;
            return endPoint.Address.IsLan();
        }
        public static bool IsLan(this IPAddress address)
        {
            if (address == null) return false;

            return IsLan(address.GetAddressBytes().AsSpan());
        }
        public static bool IsLan(this Memory<byte> address)
        {
            return IsLan(address.Span);
        }
        public static bool IsLan(Span<byte> address)
        {
            if (address.Length < 4) return false;
            if (address.Length == 4)
            {
                return address[0] == 127
               || address[0] == 10
               || (address[0] == 172 && address[1] >= 16 && address[1] <= 31)
               || (address[0] == 192 && address[1] == 168);
            }
            return address.Length == ipv6Loopback.Length && (address.SequenceEqual(ipv6Loopback.Span)
                || address.SequenceEqual(ipv6Multicast.Span)
                || (address[0] == ipv6Local.Span[0] && address[1] == ipv6Local.Span[1]));
        }


        public static bool GetIsBroadcastAddress(this IPAddress address)
        {
            return new ReadOnlySpan<byte>(address.GetAddressBytes()).GetIsBroadcastAddress();
        }
        public static bool GetIsBroadcastAddress(this ReadOnlyMemory<byte> address)
        {
            return address.Span.GetIsBroadcastAddress();
        }
        public static bool GetIsBroadcastAddress(this ReadOnlySpan<byte> address)
        {
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(address);
            return (ip >= 0xE0000000 && ip <= 0xEFFFFFFF) || ip == 0xFFFFFFFF;
        }


        public static bool GetIsAnyAddress(this IPAddress address)
        {
            return address.GetAddressBytes().AsSpan().GetIsAnyAddress();
        }
        public static bool GetIsAnyAddress(this Memory<byte> address)
        {
            return address.Span.GetIsAnyAddress();
        }
        public static bool GetIsAnyAddress(this Span<byte> address)
        {
            return (address.Length == 4 && address.SequenceEqual(anyIpArray))
                || (address.Length == 6 && address.SequenceEqual(anyIpv6Array));
        }
    }
}
