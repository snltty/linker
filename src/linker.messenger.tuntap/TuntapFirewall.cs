using linker.snat;
using linker.tun;

namespace linker.messenger.tuntap
{
    public sealed class TuntapFirewall : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Normal;

        private readonly LinkerFirewall linkerFirewall;
        public TuntapFirewall(LinkerFirewall linkerFirewall)
        {
            this.linkerFirewall = linkerFirewall;
        }

        public bool ReadAfter(ReadOnlyMemory<byte> packet)
        {
            return true;
        }

        public bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet)
        {
            if (linkerFirewall.Check(srcId, packet) == false)
            {
                return false;
            }
            return true;
        }
    }
}
