
using linker.messenger.forward.proxy;
using linker.snat;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.firewall.hooks
{
    public sealed class ForwardFirewallHook : ILinkerForwardHook
    {
        private readonly LinkerFirewall linkerFirewall;
        public ForwardFirewallHook(LinkerFirewall linkerFirewall)
        {
            this.linkerFirewall = linkerFirewall;
        }

        public bool Connect(string srcId, IPEndPoint ep, ProtocolType protocol)
        {
            return linkerFirewall.Check(srcId, ep, protocol);
        }

        public bool Forward(AsyncUserToken token)
        {
            return linkerFirewall.Check(token.Connection.RemoteMachineId, token.RealIP, ProtocolType.Tcp);
        }

        public bool Forward(AsyncUserUdpTokenTarget token)
        {
            return linkerFirewall.Check(token.Connection.RemoteMachineId, token.RealIP, ProtocolType.Udp);
        }
    }

}
