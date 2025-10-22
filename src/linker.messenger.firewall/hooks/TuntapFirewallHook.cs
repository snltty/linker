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

        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            linkerFirewall.AddAllow(packet);
            return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
        }

        public ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            if (linkerFirewall.Check(srcId, packet))
            {
                return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }
            return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.Next | LinkerTunPacketHookFlags.Write));
        }
    }
}
