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

        public bool Read(ReadOnlyMemory<byte> packet, ref bool send, ref bool writeBack)
        {
            linkerFirewall.AddAllow(packet);
            return true;
        }

        public ValueTask<(bool next, bool write)> WriteAsync(ReadOnlyMemory<byte> packet, string srcId)
        {
            bool res = linkerFirewall.Check(srcId, packet);
            return ValueTask.FromResult((res, res));
        }
    }
}
