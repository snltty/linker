using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public static class DiscoveryProtocolPacketHash
    {
        private const ulong Offset = 14_695_981_039_346_656_037UL;
        private const ulong Prime = 1_099_511_628_211UL;

        public static ulong Compute(ReadOnlySpan<byte> payload)
        {
            ulong hash = Offset;
            foreach (byte value in payload)
            {
                hash ^= value;
                hash *= Prime;
            }

            hash ^= (uint)payload.Length;
            hash *= Prime;
            return hash;
        }
    }
}
