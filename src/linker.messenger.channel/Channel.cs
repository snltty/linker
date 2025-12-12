using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.pcp;
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
        private readonly PcpTransfer pcpTransfer;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ChannelConnectionCaching channelConnectionCaching;

        public Channel(TunnelTransfer tunnelTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, ISignInClientStore signInClientStore, ChannelConnectionCaching channelConnectionCaching)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.pcpTransfer = pcpTransfer;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientStore = signInClientStore;
            this.channelConnectionCaching = channelConnectionCaching;

            //监听打洞成功
            tunnelTransfer.SetConnectedCallback(TransactionId, OnConnected);
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
            ITunnelConnection connection = null;
            //正在后台打洞
            if (tunnelTransfer.IsBackground(machineId, TransactionId) == false)
            {
                //隧道连接
                connection = await tunnelTransfer.ConnectAsync(machineId, TransactionId, denyProtocols).ConfigureAwait(false);
                if (connection == null || connection.Type == TunnelType.Relay)
                {
                    //后台打洞
                    tunnelTransfer.StartBackground(machineId, TransactionId, denyProtocols, () =>
                    {
                        return channelConnectionCaching.TryGetValue(machineId, TransactionId, out ITunnelConnection connection)
                        && connection.Connected
                        && connection.Type != TunnelType.Relay;

                    }, async (_connection) =>
                    {
                        await Task.CompletedTask;
                    }, 3, 10000);
                }
            }

            return connection;
        }

    }
}
