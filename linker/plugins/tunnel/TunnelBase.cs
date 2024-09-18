using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.client;
using linker.plugins.relay;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.plugins.tunnel
{
    public class TunnelBase
    {
        public VersionManager Version { get; } = new VersionManager();
        protected virtual string TransactionId { get; }
        protected readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();

        private readonly FileConfig config;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;
        private readonly ClientSignInTransfer clientSignInTransfer;

        public TunnelBase(FileConfig config, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, ClientSignInTransfer clientSignInTransfer)
        {
            this.config = config;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.clientSignInTransfer = clientSignInTransfer;

            //监听打洞成功
            tunnelTransfer.SetConnectedCallback(TransactionId, OnConnected);
            //监听中继成功
            relayTransfer.SetConnectedCallback(TransactionId, OnConnected);
        }
        protected virtual void Connected(ITunnelConnection connection)
        {
        }
        private void OnConnected(ITunnelConnection connection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"{TransactionId} add connection {connection.GetHashCode()} {connection.ToJson()}");

            if (connections.TryGetValue(connection.RemoteMachineId, out ITunnelConnection connectionOld) && connection.Equals(connectionOld) == false)
            {
                connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
                TimerHelper.SetTimeout(connectionOld.Dispose, 5000);
            }
            else
            {
                connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            }
            Version.Add();
            Connected(connection);
        }


        protected virtual async ValueTask<bool> WaitAsync(string machineId)
        {
            await ValueTask.CompletedTask;
            return true;
        }
        protected virtual void WaitRelease(string machineId)
        {
        }
        protected async ValueTask<ITunnelConnection> ConnectTunnel(string machineId, TunnelProtocolType denyProtocols)
        {
            if (config.Data.Client.Id == machineId)
            {
                return null;
            }
            //之前这个客户端已经连接过
            if (connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            if (await WaitAsync(machineId) == false)
            {
                return null;
            }

            try
            {
                //获得锁之前再次看看之前有没有连接成功
                if (connections.TryGetValue(machineId, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (await clientSignInTransfer.GetOnline(machineId) == false)
                {
                    return null;
                }

                connection = await RelayAndP2P(machineId, denyProtocols);
                if (connection != null)
                {
                    connections.AddOrUpdate(machineId, connection, (a, b) => connection);
                }

            }
            catch (Exception)
            {
            }
            finally
            {
                WaitRelease(machineId);
            }

            return connection;
        }
        private async Task<ITunnelConnection> RelayAndP2P(string machineId, TunnelProtocolType denyProtocols)
        {
            if (tunnelTransfer.IsBackground(machineId, TransactionId)) return null;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} relay to {machineId}");
            ITunnelConnection connection = await relayTransfer.ConnectAsync(config.Data.Client.Id, machineId, TransactionId).ConfigureAwait(false);
            if (connection != null)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} relay success,{connection.ToString()}");
                tunnelTransfer.StartBackground(machineId, TransactionId, denyProtocols, () =>
                {
                    return connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected && connection.Type == TunnelType.P2P;
                }, 3, 10000);
            }
            else
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} tunnel to {machineId}");
                connection = await tunnelTransfer.ConnectAsync(machineId, TransactionId, denyProtocols).ConfigureAwait(false);
                if (connection != null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} relay success,{connection.ToString()}");
                }
            }
            return connection;
        }

        /// <summary>
        /// 获取隧道
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, ITunnelConnection> GetConnections()
        {
            return connections;
        }
        /// <summary>
        /// 删除隧道
        /// </summary>
        /// <param name="machineId"></param>
        public void RemoveConnection(string machineId)
        {
            if (connections.TryRemove(machineId, out ITunnelConnection _connection))
            {
                try
                {
                    _connection.Dispose();
                }
                catch (Exception)
                {
                }
                Version.Add();
            }
        }
    }
}
