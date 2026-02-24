using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using System.Net;
using linker.messenger.signin;
using linker.messenger.channel;
using linker.messenger.pcp;
using linker.libs;

namespace linker.messenger.forward.proxy
{
    /// <summary>
    /// 隧道相关
    /// </summary>
    public partial class ForwardProxy : Channel, ITunnelConnectionReceiveCallback
    {
        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();

        protected override string TransactionId => "forward";

        public ForwardProxy(ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer, PcpTransfer pcpTransfer,
            SignInClientTransfer signInClientTransfer, ChannelConnectionCaching channelConnectionCaching)
            : base(tunnelTransfer, pcpTransfer, signInClientTransfer, signInClientStore, channelConnectionCaching)
        {
            TaskUdp();
        }
        /// <summary>
        /// 隧道已连接
        /// </summary>
        /// <param name="connection"></param>
        protected override void Connected(ITunnelConnection connection)
        {
            connection.BeginReceive(this, null);
        }

        /// <summary>
        /// 建立隧道
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask<bool> Tunneling(AsyncUserToken token)
        {
            if (token.ListenPort > 0)
            {
                if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
                {
                    token.ReadPacket.DstAddr = cache.DstAddr;
                    token.ReadPacket.DstPort = cache.DstPort;
                    token.IPEndPoint = new IPEndPoint(NetworkHelper.ToIP(token.ReadPacket.DstAddr), token.ReadPacket.DstPort);
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
        /// 从隧道连接发送数据到对方
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> SendToConnection(AsyncUserToken token)
        {
            if (token.Connection == null)
            {
                return false;
            }
            await token.Connection.SendAsync(token.ReadPacket.Buffer.AsMemory(token.ReadPacket.Offset, token.ReadPacket.Length)).ConfigureAwait(false);
            Add(token.Connection.RemoteMachineId, token.IPEndPoint, token.ReadPacket.Length, 0);
            return true;
        }
        private async Task<bool> SendToConnection(ITunnelConnection connection, ForwardReadPacket packet, IPEndPoint ep)
        {
            if (connection == null)
            {
                return false;
            }
            await connection.SendAsync(packet.Buffer.AsMemory(packet.Offset, packet.Length)).ConfigureAwait(false);
            Add(connection.RemoteMachineId, ep, packet.Length, 0);
            return true;
        }



        /// <summary>
        /// 隧道来数据了
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="memory"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            await InputPacket(connection, memory).ConfigureAwait(false);
        }

        /// <summary>
        /// 隧道已关闭
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public async Task Closed(ITunnelConnection connection, object userToken)
        {
            Version.Increment();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// 启动转发
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="target"></param>
        /// <param name="machineId"></param>
        /// <param name="bufferSize"></param>
        public void StartForward(IPEndPoint ep, IPEndPoint target, string machineId, byte bufferSize)
        {
            StopForward(ep.Port);
            Start(ep, bufferSize);
            caches.TryAdd(LocalEndpoint.Port, new ForwardProxyCacheInfo
            {
                Port = LocalEndpoint.Port,
                DstAddr = NetworkHelper.ToValue(target.Address),
                DstPort = (ushort)target.Port,
                MachineId = machineId,

            });
            Version.Increment();
        }
        /// <summary>
        /// 关闭转发
        /// </summary>
        /// <param name="port"></param>
        public void StopForward(int port)
        {
            caches.TryRemove(port, out ForwardProxyCacheInfo cache);
            Stop(port);
            Version.Increment();
        }

        public sealed class ForwardProxyCacheInfo
        {
            public int Port { get; set; }
            public uint DstAddr { get; set; }
            public ushort DstPort { get; set; }
            public string MachineId { get; set; }
            public ITunnelConnection Connection { get; set; }
        }
    }
}
