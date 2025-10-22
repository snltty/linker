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

        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            linkerDstMapping.ToFakeDst(packet);
            return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
        }

        public ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            linkerDstMapping.ToRealDst(packet);
            return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
        }
    }
}