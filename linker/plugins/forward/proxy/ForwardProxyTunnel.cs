using linker.config;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using System.Net;
using linker.plugins.client;
using linker.plugins.tunnel;
using linker.plugins.messenger;
using linker.plugins.relay.client;

namespace linker.plugins.forward.proxy
{
    public sealed partial class ForwardProxy : TunnelBase, ITunnelConnectionReceiveCallback
    {
        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly SemaphoreSlim slimGlobal = new SemaphoreSlim(1);

        protected override string TransactionId => "forward";

        public ForwardProxy(FileConfig config, TunnelTransfer tunnelTransfer, RelayTransfer relayTransfer, ClientSignInTransfer clientSignInTransfer, ClientSignInState clientSignInState)
            : base(config, tunnelTransfer, relayTransfer, clientSignInTransfer, clientSignInState)
        {
            TaskUdp();
        }

        protected override void Connected(ITunnelConnection connection)
        {
            BindConnectionReceive(connection);
        }
        protected override async ValueTask<bool> WaitAsync(string machineId)
        {
            //不要同时去连太多，锁以下
            await slimGlobal.WaitAsync().ConfigureAwait(false);
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim) == false)
            {
                slim = new SemaphoreSlim(1);
                locks.TryAdd(machineId, slim);
            }
            slimGlobal.Release();
            await slim.WaitAsync().ConfigureAwait(false);
            return true;
        }
        protected override void WaitRelease(string machineId)
        {
            if (locks.TryGetValue(machineId, out SemaphoreSlim slim))
            {
                slim.Release();
            }
        }

        private void BindConnectionReceive(ITunnelConnection connection)
        {
            connection.BeginReceive(this, new AsyncUserTunnelToken
            {
                Connection = connection,
                Proxy = new ProxyInfo { }
            });
        }

        /// <summary>
        /// 收到隧道数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="memory"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            AsyncUserTunnelToken token = userToken as AsyncUserTunnelToken;
            token.Proxy.DeBytes(memory);
            await ReadConnectionPack(token).ConfigureAwait(false);
        }
        /// <summary>
        /// 收到隧道关闭消息
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            Version.Add();
            await Task.CompletedTask;
        }

        /// <summary>
        /// 来一个TCP转发
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask<bool> ConnectTunnelConnection(AsyncUserToken token)
        {
            if (token.ListenPort > 0)
            {
                if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
                {
                    token.Proxy.TargetEP = cache.TargetEP;
                    cache.Connection = await ConnectTunnel(cache.MachineId, TunnelProtocolType.Udp).ConfigureAwait(false);
                    token.Connection = cache.Connection;
                }
            }
            else if (token.Connection != null)
            {
                token.Connection = await ConnectTunnel(token.Connection.RemoteMachineId, TunnelProtocolType.Udp).ConfigureAwait(false);
            }

            return true;
        }
        /// <summary>
        /// 来一个UDP转发
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask ConnectTunnelConnection(AsyncUserUdpToken token)
        {
            if (token.ListenPort > 0)
            {
                if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
                {
                    token.Proxy.TargetEP = cache.TargetEP;
                    cache.Connection = await ConnectTunnel(cache.MachineId, TunnelProtocolType.Udp).ConfigureAwait(false);
                    token.Connection = cache.Connection;
                }
            }
            else if (token.Connection != null)
            {
                token.Connection = await ConnectTunnel(token.Connection.RemoteMachineId, TunnelProtocolType.Udp).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// 启动转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="targetEP"></param>
        /// <param name="machineId"></param>
        /// <param name="bufferSize"></param>
        public void Start(IPEndPoint ep, IPEndPoint targetEP, string machineId, byte bufferSize)
        {
            StopPort(ep.Port);
            Start(ep, bufferSize);
            caches.TryAdd(LocalEndpoint.Port, new ForwardProxyCacheInfo { Port = LocalEndpoint.Port, TargetEP = targetEP, MachineId = machineId });
            Version.Add();
        }
        /// <summary>
        /// 关闭转发
        /// </summary>
        /// <param name="port"></param>
        public void StopPort(int port)
        {
            caches.TryRemove(port, out ForwardProxyCacheInfo cache);
            Stop(port);
            Version.Add();
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
