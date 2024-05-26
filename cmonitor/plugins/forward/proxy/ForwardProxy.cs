using cmonitor.client.tunnel;
using cmonitor.plugins.relay;
using cmonitor.plugins.tunnel;
using common.libs;
using System.Collections.Concurrent;
using System.Net;

namespace cmonitor.plugins.forward.proxy
{
    public sealed class ForwardProxy : TunnelProxy
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;

        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();
        private readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public ForwardProxy(TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;

            tunnelTransfer.SetConnectedCallback("forward", OnConnected);
            relayTransfer.SetConnectedCallback("forward", OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            connections.AddOrUpdate(connection.RemoteMachineName, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }

        protected override async Task<bool> ConnectTcp(AsyncUserToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineName);
                token.Connection = cache.Connection;
            }
            return true;
        }
        protected override async Task ConnectUdp(AsyncUserUdpToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineName);
                token.Connection = cache.Connection;
            }

        }

        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        private async Task<ITunnelConnection> ConnectTunnel(string machineName)
        {
            if (connections.TryGetValue(machineName, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            await slimGlobal.WaitAsync();
            if (locks.TryGetValue(machineName, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                locks.TryAdd(machineName, slim);
            }
            slimGlobal.Release();
            await slim.WaitAsync();

            try
            {
                if (connections.TryGetValue(machineName, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward tunnel to {machineName}");
                connection = await tunnelTransfer.ConnectAsync(machineName, "forward");
                if (connection != null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward tunnel to {machineName} success");
                }
                if (connection == null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward relay to {machineName}");

                    connection = await relayTransfer.ConnectAsync(machineName, "forward");
                    if (connection != null)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG) Logger.Instance.Debug($"forward relay to {machineName} success");
                    }
                }
                if (connection != null)
                {
                    connections.AddOrUpdate(machineName, connection, (a, b) => connection);
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


        public void Start(int port, IPEndPoint targetEP, string machineName)
        {
            Stop(port);
            caches.TryAdd(port, new ForwardProxyCacheInfo { Port = port, TargetEP = targetEP, MachineName = machineName });
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
            public string MachineName { get; set; }

            public ITunnelConnection Connection { get; set; }
        }
    }
}
