using linker.snat;
using static linker.snat.LinkerDstMapping;

namespace linker.tun
{
    internal sealed class LanMap : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Lowest;

        private readonly LinkerDstMapping linkerDstMapping = new LinkerDstMapping();
        public void SetMap(DstMapInfo[] maps)
        {
            linkerDstMapping.SetDsts(maps);
        }

        public bool ReadAfter(ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToFakeDst(packet);
            return true;
        }

        public bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToRealDst(packet);
            return true;
        }
    }
}
