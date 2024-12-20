using linker.tunnel.transport;
using linker.plugins.client;
using linker.messenger;
using System.Security.Cryptography.X509Certificates;

namespace linker.plugins.tunnel
{
    public sealed class PlusTunnelMessengerAdapter
    {
        public PlusTunnelMessengerAdapter(ClientSignInState clientSignInState,TunnelConfigTransfer tunnelConfigTransfer, TunnelMessengerAdapter tunnelMessengerAdapter)
        {

            clientSignInState.NetworkEnabledHandle += (times) => tunnelMessengerAdapter.RefreshPortMap(tunnelConfigTransfer.PortMapLan, tunnelConfigTransfer.PortMapWan);
            tunnelConfigTransfer.OnChanged += () => tunnelMessengerAdapter.RefreshPortMap(tunnelConfigTransfer.PortMapLan, tunnelConfigTransfer.PortMapWan);
        }
    }
    public sealed class PlusTunnelMessengerAdapterStore : ITunnelMessengerAdapterStore
    {
        public IConnection SignConnection => clientSignInState.Connection;
        public NetworkInfo Network => GetLocalConfig();
        public X509Certificate2 Certificate => tunnelConfigTransfer.Certificate;
        public List<TunnelTransportItemInfo> TunnelTransports => tunnelConfigTransfer.Transports;

        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        public PlusTunnelMessengerAdapterStore(ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer, TunnelConfigTransfer tunnelConfigTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;

           
        }
       
        public bool SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            tunnelConfigTransfer.SetTransports(list);
            return true;
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
