using linker.tunnel.transport;
using linker.plugins.client;
using linker.messenger;
using System.Security.Cryptography.X509Certificates;
using linker.tunnel;

namespace linker.plugins.tunnel
{
    public sealed class PlusTunnelMessengerAdapterStore : ITunnelMessengerAdapterStore
    {
        public IConnection SignConnection => clientSignInState.Connection;
        public X509Certificate2 Certificate => tunnelConfigTransfer.Certificate;
        public List<TunnelTransportItemInfo> TunnelTransports => tunnelConfigTransfer.Transports;
        public int RouteLevelPlus => tunnelConfigTransfer.RouteLevelPlus;

        public int PortMapPrivate => tunnelConfigTransfer.PortMapLan;
        public int PortMapPublic => tunnelConfigTransfer.PortMapWan;

        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        public PlusTunnelMessengerAdapterStore(ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer, TunnelConfigTransfer tunnelConfigTransfer,TunnelTransfer tunnelTransfer)
        {
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;
            this.tunnelConfigTransfer = tunnelConfigTransfer;

            clientSignInState.NetworkEnabledHandle += (times) => tunnelTransfer.Refresh();
            tunnelConfigTransfer.OnChanged += () => tunnelTransfer.Refresh();
        }
        public async Task<bool> SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            tunnelConfigTransfer.SetTransports(list);
            return await Task.FromResult(true);
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports()
        {
            return await Task.FromResult(tunnelConfigTransfer.Transports);
        }

    }
}
