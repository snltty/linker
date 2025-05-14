using linker.snat;
using linker.tun;
using System.Net.Sockets;
using static linker.snat.LinkerSrcNat;

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
