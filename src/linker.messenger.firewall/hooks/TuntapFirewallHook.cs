using linker.nat;
using linker.tun.hook;

namespace linker.messenger.firewall.hooks
{
    public sealed class TuntapFirewallHook : ILinkerTunPacketHook
    {
        public string Name => "Firewall";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Normal;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Normal;

        private readonly LinkerFirewall linkerFirewall;
        public TuntapFirewallHook(LinkerFirewall linkerFirewall)
        {
            this.linkerFirewall = linkerFirewall;
        }

        public unsafe bool Read(ReadOnlyMemory<byte> packet,  ref bool send, ref bool writeBack)
        {
            linkerFirewall.AddAllow(packet);
            return true;
        }

        public unsafe bool Write(ReadOnlyMemory<byte> packet, string srcId, ref bool write)
        {
            bool res = linkerFirewall.Check(srcId, packet);
            if (res == false) write = false;
            return res;
        }
    }
}
