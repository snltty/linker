using linker.snat;
using linker.tun;

namespace linker.messenger.firewall.hooks
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
            return linkerFirewall.Check(srcId, packet);
        }
    }
}
