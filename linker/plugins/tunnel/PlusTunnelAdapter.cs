using linker.tunnel.transport;
using linker.libs;
using linker.plugins.client;
using linker.tunnel.wanport;
using linker.tunnel;
using linker.messenger;
using linker.messenger.tunnel;

namespace linker.plugins.tunnel
{
    public sealed class PlusTunnelAdapter : TunnelMessengerAdapter
    {
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;


        public PlusTunnelAdapter(ClientSignInState clientSignInState, IMessengerSender messengerSender,
            TunnelExcludeIPTransfer excludeIPTransfer, ClientConfigTransfer clientConfigTransfer, TunnelConfigTransfer tunnelConfigTransfer,
            TunnelWanPortTransfer tunnelWanPortTransfer, TunnelUpnpTransfer tunnelUpnpTransfer, TunnelTransfer tunnelTransfer, ISerializer serializer)
            : base(messengerSender, excludeIPTransfer, tunnelWanPortTransfer, tunnelUpnpTransfer, tunnelTransfer, serializer)
        {
            this.clientConfigTransfer = clientConfigTransfer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;

            clientSignInState.NetworkEnabledHandle += (times) => RefreshPortMap(tunnelConfigTransfer.PortMapLan, tunnelConfigTransfer.PortMapWan);
            tunnelConfigTransfer.OnChanged += () => RefreshPortMap(tunnelConfigTransfer.PortMapLan, tunnelConfigTransfer.PortMapWan);


            this.GetSignConnection = () => clientSignInState.Connection;
            this.GetNetwork = GetLocalConfig;
            this.Certificate = () => tunnelConfigTransfer.Certificate;
            this.GetTunnelTransports = () => tunnelConfigTransfer.Transports;
            this.SetTunnelTransports = (list, update) => tunnelConfigTransfer.SetTransports(list);
        }

        private NetworkInfo GetLocalConfig()
        {
            return new NetworkInfo
            {
                LocalIps = tunnelConfigTransfer.LocalIPs,
                RouteLevel = tunnelConfigTransfer.RouteLevel,
                MachineId = clientConfigTransfer.Id
            };
        }
    }
}
