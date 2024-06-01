using cmonitor.client.running;
using cmonitor.client.tunnel;
using cmonitor.plugins.relay;
using cmonitor.plugins.tunnel;
using common.libs;
using System.Collections.Concurrent;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxy : TunnelProxy
    {
        private readonly RunningConfig runningConfig;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;

        private readonly ConcurrentDictionary<string, ITunnelConnection> dicConnections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> dicLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public ViewerProxy(RunningConfig runningConfig, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.runningConfig = runningConfig;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;

            Start(0);
            Logger.Instance.Info($"start viewer proxy, listen port : {LocalEndpoint}");

            tunnelTransfer.SetConnectedCallback("viewer", OnConnected);
            relayTransfer.SetConnectedCallback("viewer", OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            dicConnections.AddOrUpdate(connection.RemoteMachineName, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }


        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        protected override async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            token.Proxy.TargetEP = runningConfig.Data.Viewer.ConnectEP;
            token.Connection = await ConnectTunnel(runningConfig.Data.Viewer.ServerMachine);

            return true;
        }

        protected override async ValueTask CheckTunnelConnection(AsyncUserToken token)
        {
            if (token.Connection == null || token.Connection.Connected == false)
            {
                token.Proxy.TargetEP = runningConfig.Data.Viewer.ConnectEP;
                token.Connection = await ConnectTunnel(runningConfig.Data.Viewer.ServerMachine);
            }
        }


        private async ValueTask<ITunnelConnection> ConnectTunnel(string targetName)
        {
            if (dicConnections.TryGetValue(targetName, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            await slimGlobal.WaitAsync();
            if (dicLocks.TryGetValue(targetName, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                dicLocks.TryAdd(targetName, slim);
            }
            slimGlobal.Release();

            await slim.WaitAsync();

            try
            {
                if (dicConnections.TryGetValue(targetName, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"viewer tunnel to {targetName}");

                connection = await tunnelTransfer.ConnectAsync(targetName, "viewer");
                if (connection != null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"viewer tunnel success,{connection.ToString()}");
                }
                if (connection == null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"viewer relay to {targetName}");

                    connection = await relayTransfer.ConnectAsync(targetName, "viewer");
                    if (connection != null)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"viewer relay success,{connection.ToString()}");
                    }
                }
                if (connection != null)
                {
                    dicConnections.AddOrUpdate(targetName, connection, (a, b) => connection);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
            return connection;
        }
    }
}
