using linker.snat;
using static linker.snat.LinkerDstMapping;

namespace linker.tun
{
    internal sealed class LanMap : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Lowest;


        private readonly LinkerDstMapping linkerDstMapping = new LinkerDstMapping();
        private bool checksum = true;
        public void SetMap(DstMapInfo[] maps,bool checksum = true)
        {
            linkerDstMapping.SetDsts(maps);
            this.checksum = checksum;
        }

        public bool ReadAfter(ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToFakeDst(packet);
            return true;
        }

        public bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToRealDst(packet, checksum);
            return true;
        }
    }
}
