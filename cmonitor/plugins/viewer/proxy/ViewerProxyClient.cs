using cmonitor.client.running;
using cmonitor.client.tunnel;
using cmonitor.config;
using cmonitor.plugins.relay;
using cmonitor.plugins.tunnel;
using common.libs;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxyClient : ViewerProxy
    {
        private readonly RunningConfig runningConfig;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;
        private readonly Config config;

        private ITunnelConnection connection;

        public ViewerProxyClient(RunningConfig runningConfig, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, Config config)
        {
            this.runningConfig = runningConfig;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.config = config;

            Start(0);
            Logger.Instance.Info($"start viewer proxy, port : {LocalEndpoint.Port}");

            tunnelTransfer.SetConnectCallback("viewer", BindConnectionReceive);
            relayTransfer.SetConnectCallback("viewer", BindConnectionReceive);
        }

        protected override async Task Connect(AsyncUserToken token)
        {
            token.Proxy.TargetEP = runningConfig.Data.Viewer.ConnectEP;
            token.Connection = connection;
            if (connection == null || connection.Connected == false)
            {
                connection = await tunnelTransfer.ConnectAsync(runningConfig.Data.Viewer.ServerMachine, "viewer");
                if (connection == null)
                {
                    connection = await relayTransfer.ConnectAsync(runningConfig.Data.Viewer.ServerMachine, "viewer", config.Data.Client.Relay.SecretKey);
                }
                if (connection != null)
                {
                    BindConnectionReceive(connection);
                    token.Connection = connection;
                }
            }
        }
    }
}
