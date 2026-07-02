using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;
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
        public ITunnelConnection Add(ITunnelConnection connection)
        {
            if (Connections.TryGetValue(connection.TransactionId, out ConcurrentDictionary<string, ITunnelConnection> _connections) == false)
            {
                _connections = new ConcurrentDictionary<string, ITunnelConnection>();
                Connections.TryAdd(connection.TransactionId, _connections);
            }
            _connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            Version.Increment();
            return connection;
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
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly ChannelConnectionCaching channelConnectionCaching;
        private readonly OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();


        public Channel(TunnelTransfer tunnelTransfer,
            SignInClientTransfer signInClientTransfer, ISignInClientStore signInClientStore, ChannelConnectionCaching channelConnectionCaching)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.signInClientTransfer = signInClientTransfer;
            this.signInClientStore = signInClientStore;
            this.channelConnectionCaching = channelConnectionCaching;
            tunnelTransfer.SetConnectedCallback(TransactionId, OnConnected);

        }
        public virtual void Add(ITunnelConnection connection)
        {
        }
        protected virtual void Connected(ITunnelConnection connection)
        {
        }
        private void OnConnected(ITunnelConnection connection, TunnelTransportInfo info)
        {
            if (connection == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"{TransactionId} add connection {connection.GetHashCode()} {connection.ToJson()}");

            if (channelConnectionCaching.TryGetValue(connection.RemoteMachineId, TransactionId, out ITunnelConnection connectionOld) && connection.Equals(connectionOld) == false)
            {
                TimerHelper.SetTimeout(connectionOld.Dispose, 5000);
            }
            connection = channelConnectionCaching.Add(connection);
            Version.Increment();

            Connected(connection);
            Add(connection);
        }

        protected async ValueTask<ITunnelConnection> ConnectTunnel(string machineId, Dictionary<string, string> configures)
        {
            if (channelConnectionCaching.TryGetValue(machineId, TransactionId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            if (operatingMultipleManager.StartOperation($"{machineId}@{TransactionId}"))
            {
                _ = RelayAndP2P(machineId, configures).ContinueWith((result) =>
                {
                    operatingMultipleManager.StopOperation($"{machineId}@{TransactionId}");
                    if (result.Result != null)
                    {
                        channelConnectionCaching.Add(result.Result);
                    }
                }).ConfigureAwait(false);
            }

            return null;
        }
        private async Task<ITunnelConnection> RelayAndP2P(string machineId, Dictionary<string, string> configures)
        {
            if (signInClientStore.Id == machineId)
            {
                return null;
            }
            if (await signInClientTransfer.GetOnline(machineId).ConfigureAwait(false) == false)
            {
                return null;
            }

            ITunnelConnection connection = await tunnelTransfer.ConnectAsync(machineId, TransactionId, configures).ConfigureAwait(false);
            if (connection != null && connection.Type != TunnelType.P2P)
            {
                tunnelTransfer.StartBackground(machineId, TransactionId, configures, () =>
                {
                    return channelConnectionCaching.TryGetValue(machineId, TransactionId, out ITunnelConnection _connection)
                    && _connection.Connected
                    && _connection.Type == TunnelType.P2P;

                }, (_connection) => Task.CompletedTask, 65535, 10000);
            }

            return connection;
        }
    }
}
