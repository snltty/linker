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

        public ForwardProxy(TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;
        }

        protected override async Task<bool> ConnectTcp(AsyncUserToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                if (cache.Connection == null || cache.Connection.Connected == false)
                {
                    cache.Connection = await ConnectTunnel(cache.MachineName);
                }
                token.Connection = cache.Connection;
            }
            return true;
        }
        protected override async Task ConnectUdp(AsyncUserUdpToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                if (cache.Connection == null || cache.Connection.Connected == false)
                {
                    cache.Connection = await ConnectTunnel(cache.MachineName);
                }
                token.Connection = cache.Connection;
            }

        }
        private async Task<ITunnelConnection> ConnectTunnel(string machineName)
        {
            if (connections.TryGetValue(machineName, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                Logger.Instance.Debug($"viewer tunnel to {machineName}");
            connection = await tunnelTransfer.ConnectAsync(machineName, "viewer");
            if (connection != null)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Debug($"viewer tunnel to {machineName} success");
            }
            if (connection == null)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Debug($"viewer relay to {machineName}");
                connection = await relayTransfer.ConnectAsync(machineName, "viewer");
                if (connection != null)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        Logger.Instance.Debug($"viewer relay to {machineName} success");
                }
            }
            if (connection != null)
                connections.TryAdd(machineName, connection);
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
