using linker.nat;
using static linker.nat.LinkerDstMapping;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanMap : ILinkerTunPacketHook
    {
        public string Name => "Map";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Highest;
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

        public ValueTask<(bool next, bool write)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            linkerDstMapping.ToRealDst(packet);
            return ValueTask.FromResult((true, true));
        }
    }
} 