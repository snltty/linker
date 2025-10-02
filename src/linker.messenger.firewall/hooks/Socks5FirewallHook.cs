using linker.messenger.socks5;
using linker.nat;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.firewall.hooks
{
    public sealed class Socks5FirewallHook : ILinkerSocks5Hook
    {
        private readonly LinkerFirewall linkerFirewall;
        public Socks5FirewallHook(LinkerFirewall linkerFirewall)
        {
            this.linkerFirewall = linkerFirewall;
        }

        public bool Connect(string srcId, IPEndPoint ep, ProtocolType protocol)
        {
            return linkerFirewall.Check(srcId, ep, protocol);
        }

        public bool Forward(AsyncUserToken token)
        {
            return linkerFirewall.Check(token.Connection.RemoteMachineId, token.IPEndPoint, ProtocolType.Tcp);
        }

    }

}
