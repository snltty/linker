using linker.nat;
using static linker.nat.LinkerDstMapping;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanMap : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Lowest;


        private readonly LinkerDstMapping linkerDstMapping = new LinkerDstMapping();
        public void SetMap(DstMapInfo[] maps)
        {
            linkerDstMapping.SetDsts(maps);
        }

        public bool Read(ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToFakeDst(packet);
            return true;
        }

        public bool Write(string srcId, ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToRealDst(packet);
            return true;
        }
    }
}
