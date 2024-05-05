using cmonitor.client.running;
using cmonitor.config;
using cmonitor.plugins.relay;
using cmonitor.plugins.relay.transport;
using cmonitor.plugins.tunnel;
using cmonitor.plugins.tunnel.transport;
using common.libs;
using System.Net.Sockets;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxyClient : ViewerProxy
    {
        private readonly RunningConfig runningConfig;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;
        private readonly Config config;

        private Socket tunnelSocket;

        public ViewerProxyClient(RunningConfig runningConfig, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, Config config)
        {
            this.runningConfig = runningConfig;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.config = config;

            Start(0);
            Logger.Instance.Info($"start viewer proxy, port : {LocalEndpoint.Port}");

            Tunnel();
        }

        protected override async Task Connect(AsyncUserToken token, ProxyInfo proxyInfo)
        {
            if (tunnelSocket == null || tunnelSocket.Connected == false)
            {
                TunnelTransportState state = await tunnelTransfer.ConnectAsync(runningConfig.Data.Viewer.ServerMachine, "viewer");
                if (state != null)
                {
                    if (state.TransportType == ProtocolType.Tcp)
                    {
                        tunnelSocket = state.ConnectedObject as Socket;
                        BindReceiveTarget(tunnelSocket, token.SourceSocket);
                        goto exit;
                    }
                }
                RelayTransportState relayState = await relayTransfer.ConnectAsync(runningConfig.Data.Viewer.ServerMachine, "viewer", config.Data.Client.Relay.SecretKey);
                if (relayState != null)
                {
                    tunnelSocket = relayState.Socket;
                    BindReceiveTarget(tunnelSocket, token.SourceSocket);
                    goto exit;
                }

                tunnelSocket = null;
            }
        exit:
            token.TargetSocket = tunnelSocket;
            proxyInfo.TargetEP = runningConfig.Data.Viewer.ConnectEP;
        }

        private void Tunnel()
        {
            tunnelTransfer.OnConnected += (TunnelTransportState state) =>
            {
                if (state != null && state.TransportType == ProtocolType.Tcp && state.TransactionId == "viewer")
                {
                    BindReceiveTarget(state.ConnectedObject as Socket, null);
                }
            };
            relayTransfer.OnConnected += (RelayTransportState state) =>
            {
                if (state != null && state.Info.TransactionId == "viewer")
                {
                    BindReceiveTarget(state.Socket, null);
                }
            };
        }
    }
}
