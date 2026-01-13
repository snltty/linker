using System;
using System.Buffers.Binary;
using System.Net;

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
    }
}
