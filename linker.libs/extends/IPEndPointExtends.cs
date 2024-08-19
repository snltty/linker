using System;
using System.Buffers.Binary;
using System.Net;

namespace linker.libs.extends
{
    public static class IPEndPointExtends
    {
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
            return address[3] == 255 || (ip >= 0xE0000000 && ip <= 0xEFFFFFFF) || ip == 0xFFFFFFFF;
        }
    }
}
