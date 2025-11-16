using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.pcp;
using linker.messenger.relay.client;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.messenger.channel
{
    public sealed class ChannelConnectionCaching
    {
        public VersionManager Version { get; } = new VersionManager();
        public ConcurrentDictionary<string, ConcurrentDictionary<string, ITunnelConnection>> Connections { get; } = new();

        public ConcurrentDictionary<string, ITunnelConnection> this[string transactionId]
        {
            get
            {
                if (Connections.TryGetValue(transactionId, out ConcurrentDictionary<string, ITunnelConnection> _connections) == false)
                {
                    _connections = new ConcurrentDictionary<string, ITunnelConnection>();
                    Connections.TryAdd(transactionId, _connections);
                }
                return _connections;
            }
        }

        public bool TryGetValue(string machineId, string transactionId, out ITunnelConnection connection)
        {
            connection = null;
            if (Connections.TryGetValue(transactionId, out ConcurrentDictionary<string, ITunnelConnection> _connections))
            {
                return _connections.TryGetValue(machineId, out connection);
            }
            return false;
        }
        public void Add(ITunnelConnection connection)
        {
            if (Connections.TryGetValue(connection.TransactionId, out ConcurrentDictionary<string, ITunnelConnection> _connections) == false)
            {
                _connections = new ConcurrentDictionary<string, ITunnelConnection>();
                Connections.TryAdd(connection.TransactionId, _connections);
            }
            _connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            Version.Increment();
        }
        public void Remove(string machineId, string transactionId)
        {
            if (Connections.TryGetValue(transactionId, out ConcurrentDictionary<string, ITunnelConnection> _connections))
            {
                if (_connections.TryRemove(machineId, out ITunnelConnection _connection))
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
    public class Channel
    {
        public VersionManager Version => channelConnectionCaching.Version;
        public ConcurrentDictionary<string, ITunnelConnection> Connections => channelConnectionCaching[TransactionId];

        protected virtual string TransactionId { get; }

        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayClientTransfer relayTransfer;
        private readonly PcpTransfer pcpTransfer;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ChannelConnectionCaching channelConnectionCaching;

        public Channel(TunnelTransfer tunnelTransfer, RelayClientTransfer relayTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, ISignInClientStore signInClientStore, ChannelConnectionCaching channelConnectionCaching)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
            this.pcpTransfer = pcpTransfer;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientStore = signInClientStore;
            this.channelConnectionCaching = channelConnectionCaching;

            //监听打洞成功
            tunnelTransfer.SetConnectedCallback(TransactionId, OnConnected);
            //监听中继成功
            relayTransfer.SetConnectedCallback(TransactionId, OnConnected);
            //监听节点中继成功回调
            pcpTransfer.SetConnectedCallback(TransactionId, OnConnected);

        }
        public virtual void Add(ITunnelConnection connection)
        {
        }
        protected virtual void Connected(ITunnelConnection connection)
        {
        }
        private void OnConnected(ITunnelConnection connection)
        {
            if (connection == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"{TransactionId} add connection {connection.GetHashCode()} {connection.ToJson()}");

            if (channelConnectionCaching.TryGetValue(connection.RemoteMachineId, TransactionId, out ITunnelConnection connectionOld) && connection.Equals(connectionOld) == false)
            {
                TimerHelper.SetTimeout(connectionOld.Dispose, 5000);
            }
            channelConnectionCaching.Add(connection);
            Version.Increment();

            Connected(connection);
            Add(connection);
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
            if (channelConnectionCaching.TryGetValue(machineId, TransactionId, out ITunnelConnection connection) && connection.Connected)
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
                if (channelConnectionCaching.TryGetValue(machineId, TransactionId, out connection) && connection.Connected)
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
                    channelConnectionCaching.Add(connection);
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
                    return channelConnectionCaching.TryGetValue(machineId, TransactionId, out ITunnelConnection connection) && connection.Connected && connection.Type == TunnelType.P2P;
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

    }
}
