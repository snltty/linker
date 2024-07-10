using linker.config;
using linker.plugins.relay;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.proxy;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;

namespace linker.plugins.forward.proxy
{
    public sealed class ForwardProxy : TunnelProxy
    {
        private readonly ConfigWrap config;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly RelayTransfer relayTransfer;

        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();
        private readonly ConcurrentDictionary<string, ITunnelConnection> connections = new ConcurrentDictionary<string, ITunnelConnection>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public ForwardProxy(ConfigWrap config, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer)
        {
            this.config = config;
            this.tunnelTransfer = tunnelTransfer;
            this.relayTransfer = relayTransfer;

            //监听打洞成功
            tunnelTransfer.SetConnectedCallback("forward", OnConnected);
            //监听中继成功
            relayTransfer.SetConnectedCallback("forward", OnConnected);
        }
        private void OnConnected(ITunnelConnection connection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"TryAdd {connection.GetHashCode()} {connection.TransactionId} {connection.ToJson()}");

            //把隧道对象添加到缓存，方便下次直接获取
            connections.AddOrUpdate(connection.RemoteMachineId, connection, (a, b) => connection);
            BindConnectionReceive(connection);
        }

        /// <summary>
        /// 来一个TCP转发
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineId).ConfigureAwait(false);
                token.Connection = cache.Connection;
            }
            return true;
        }
        /// <summary>
        /// 来一个UDP转发
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
            {
                token.Proxy.TargetEP = cache.TargetEP;
                cache.Connection = await ConnectTunnel(cache.MachineId).ConfigureAwait(false);
                token.Connection = cache.Connection;
            }
        }


        SemaphoreSlim slimGlobal = new SemaphoreSlim(1);
        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        private async ValueTask<ITunnelConnection> ConnectTunnel(string machineId)
        {
            //之前这个客户端已经连接过
            if (connections.TryGetValue(machineId, out ITunnelConnection connection) && connection.Connected)
            {
                return connection;
            }
            //不要同时去连太多，锁以下
            await slimGlobal.WaitAsync().ConfigureAwait(false);
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                locks.TryAdd(machineId, slim);
            }
            slimGlobal.Release();
            await slim.WaitAsync().ConfigureAwait(false);
            try
            {
                //获得锁之前再次看看之前有没有连接成功
                if (connections.TryGetValue(machineId, out connection) && connection.Connected)
                {
                    return connection;
                }

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"forward tunnel to {machineId}");
                //打洞
                connection = await tunnelTransfer.ConnectAsync(machineId, "forward").ConfigureAwait(false);
                if (connection != null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"forward tunnel to {machineId} success");
                }
                //打洞失败
                if (connection == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"forward relay to {machineId}");
                    //转入后台打洞
                    tunnelTransfer.StartBackground(machineId, "forward");
                    //尝试中继
                    connection = await relayTransfer.ConnectAsync(config.Data.Client.Id, machineId, "forward").ConfigureAwait(false);
                    if (connection != null)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"forward relay to {machineId} success");
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


        public void Start(IPEndPoint ep, IPEndPoint targetEP, string machineId,byte bufferSize)
        {
            Stop(ep.Port);
            base.Start(ep, bufferSize);
            caches.TryAdd(LocalEndpoint.Port, new ForwardProxyCacheInfo { Port = LocalEndpoint.Port, TargetEP = targetEP, MachineId = machineId });
        }
        public override void Stop(int port)
        {
            if (caches.TryRemove(port, out ForwardProxyCacheInfo cache))
            {
                base.Stop(port);
            }
        }

        public ConcurrentDictionary<string, ITunnelConnection> GetConnections()
        {
            return connections;
        }
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
