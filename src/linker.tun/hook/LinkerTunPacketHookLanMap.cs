using linker.nat;
using static linker.nat.LinkerDstMapping;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanMap : ILinkerTunPacketHook
    {
        public string Name => "Map";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Lowest;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Lowest;


        private readonly LinkerDstMapping linkerDstMapping = new LinkerDstMapping();
        public void SetMap(DstMapInfo[] maps)
        {
            linkerDstMapping.SetDsts(maps);
        }

        public bool Read(ReadOnlyMemory<byte> packet, ref bool send, ref bool writeBack)
        {
            linkerDstMapping.ToFakeDst(packet);
            return true;
        }

        public bool Write(ReadOnlyMemory<byte> packet, string srcId, ref bool write)
        {
            linkerDstMapping.ToRealDst(packet);
            return true;
        }
    }
}
