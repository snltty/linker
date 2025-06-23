using linker.snat;
using linker.tun;

namespace linker.messenger.tuntap
{
    public sealed class TuntapFirewallHook : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Normal;

        private readonly LinkerFirewall linkerFirewall;
        public TuntapFirewallHook(LinkerFirewall linkerFirewall)
        {
            this.linkerFirewall = linkerFirewall;
        }

        public unsafe bool ReadAfter(ReadOnlyMemory<byte> packet)
        {
            linkerFirewall.AddAllow(packet);

            return true;
        }

        public unsafe bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet)
        {
            if (linkerFirewall.Check(srcId, packet) == false)
            {
                return false;
            }
            return true;
        }
    }
}
