using linker.libs;
using linker.libs.extends;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using linker.messenger.relay.client;
using linker.messenger.pcp;
using linker.messenger.signin;
using linker.libs.timer;

namespace linker.messenger.channel
{
    public class Channel
    {
        public VersionManager Version { get; } = new VersionManager();
        protected virtual string TransactionId { get; }
        protected readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();

        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayClientTransfer relayTransfer;
        private readonly PcpTransfer pcpTransfer;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly IRelayClientStore relayClientStore;

        public Channel(TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer, SignInClientTransfer signInClientTransfer, ISignInClientStore signInClientStore, IRelayClientStore relayClientStore)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.pcpTransfer = pcpTransfer;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientStore = signInClientStore;
            this.relayClientStore = relayClientStore;

            //监听打洞成功
            tunnelTransfer.SetConnectedCallback(TransactionId, OnConnected);
            //监听中继成功
            relayTransfer.SetConnectedCallback(TransactionId, OnConnected);
            //监听节点中继成功回调
            pcpTransfer.SetConnectedCallback(TransactionId, OnConnected);

        }
        protected virtual void Connected(ITunnelConnection connection)
        {
        }
        private void OnConnected(ITunnelConnection connection)
        {
            if (connection == null) return;

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
            Version.Increment();

            Connected(connection);

           
                pcpTransfer.AddConnection(connection);
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
            if (signInClientStore.Id == machineId)
            {
                return null;
            }
            //之前这个客户端已经连接过
            if (connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }
            try
            {
                //锁
                if (await WaitAsync(machineId).ConfigureAwait(false) == false)
                {
                    return null;
                }

                //获得锁再次看看之前有没有连接成功
                if (connections.TryGetValue(machineId, out connection) && connection.Connected)
                {
                    return connection;
                }
                //不在线就不必连了
                if (await signInClientTransfer.GetOnline(machineId).ConfigureAwait(false) == false)
                {
                    return null;
                }

                connection = await RelayAndP2P(machineId, denyProtocols).ConfigureAwait(false);

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
            //中继
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} relay to {machineId}");
            ITunnelConnection connection = await relayTransfer.ConnectAsync(signInClientStore.Id, machineId, TransactionId, denyProtocols).ConfigureAwait(false);
            if (connection != null)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} relay success,{connection.ToString()}");
            }

            //正在后台打洞
            if (tunnelTransfer.IsBackground(machineId, TransactionId))
            {
                return connection;
            }

            if (connection != null)
            {
                //后台打洞
                tunnelTransfer.StartBackground(machineId, TransactionId, denyProtocols, () =>
                {
                    return connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected && connection.Type == TunnelType.P2P;
                }, async (_connection) =>
                {
                    //后台打洞失败，pcp
                    if (_connection == null)
                    {
                        await pcpTransfer.ConnectAsync(machineId, TransactionId, denyProtocols).ConfigureAwait(false);
                    }
                }, 3, 10000);
            }
            else
            {
                //打洞
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} p2p to {machineId}");
                connection = await tunnelTransfer.ConnectAsync(machineId, TransactionId, denyProtocols).ConfigureAwait(false);
                if (connection != null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} p2p success,{connection.ToString()}");
                }
                if (connection == null)
                {
                    //pcp
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} pcp to {machineId}");
                    connection = await pcpTransfer.ConnectAsync(machineId, TransactionId, denyProtocols).ConfigureAwait(false);
                }
                if (connection != null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"{TransactionId} pcp success,{connection.ToString()}");
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
                Version.Increment();
            }
        }
    }
}
