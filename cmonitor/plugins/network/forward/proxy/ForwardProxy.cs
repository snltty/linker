using cmonitor.config;
using cmonitor.plugins.relay;
using cmonitor.tunnel;
using cmonitor.tunnel.connection;
using cmonitor.tunnel.proxy;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;

namespace cmonitor.plugins.forward.proxy
{
    public sealed class ForwardProxy : TunnelProxy
    {
        private readonly Config config;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;

        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();
        private readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public ForwardProxy(Config config,TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.config = config;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;

            tunnelTransfer.SetConnectedCallback("forward", OnConnected);
            relayTransfer.SetConnectedCallback("forward", OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Warning($"TryAdd {connection.GetHashCode()} {connection.TransactionId} {connection.ToJson()}");

            connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }

        protected override async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineId);
                token.Connection = cache.Connection;
            }
            return true;
        }
        protected override async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineId);
                token.Connection = cache.Connection;
            }
        }


        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        private async ValueTask<ITunnelConnection> ConnectTunnel(string machineId)
        {
            if (connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            await slimGlobal.WaitAsync();
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                locks.TryAdd(machineId, slim);
            }
            slimGlobal.Release();
            await slim.WaitAsync();

            try
            {
                if (connections.TryGetValue(machineId, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward tunnel to {machineId}");
                connection = await tunnelTransfer.ConnectAsync(machineId, "forward");
                if (connection != null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward tunnel to {machineId} success");
                }
                if (connection == null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward relay to {machineId}");

                    connection = await relayTransfer.ConnectAsync(config.Data.Client.Id,machineId, "forward");
                    if (connection != null)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward relay to {machineId} success");
                    }
                }
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
                slim.Release();
            }

            return connection;
        }


        public void Start(int port, IPEndPoint targetEP, string machineId)
        {
            Stop(port);
            caches.TryAdd(port, new ForwardProxyCacheInfo { Port = port, TargetEP = targetEP, MachineId = machineId });
            base.Start(port);
        }
        public override void Stop(int port)
        {
            if (caches.TryRemove(port, out ForwardProxyCacheInfo cache))
            {
                base.Stop(port);
            }
        }

        public sealed class ForwardProxyCacheInfo
        {
            public int Port { get; set; }
            public IPEndPoint TargetEP { get; set; }
            public string MachineId { get; set; }

            public ITunnelConnection Connection { get; set; }
        }
    }
}
