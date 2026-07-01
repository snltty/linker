using linker.forward;
using linker.libs;
using linker.messenger.channel;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.forward
{
    public partial class ForwardProxy : linker.forward.ForwardProxy
    {
        private readonly ConcurrentDictionary<int, ForwardProxyCacheInfo> caches = new ConcurrentDictionary<int, ForwardProxyCacheInfo>();
        protected override string TransactionId => "forward";

        public ForwardProxy(ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer,
            SignInClientTransfer signInClientTransfer, ChannelConnectionCaching channelConnectionCaching)
            : base(signInClientStore, tunnelTransfer, signInClientTransfer, channelConnectionCaching)
        {
        }
        protected override async ValueTask<int> Tunneling(AsyncUserToken token, ProtocolType protocol)
        {
            if (token.ListenPort > 0)
            {
                if (caches.TryGetValue(token.ListenPort, out ForwardProxyCacheInfo cache))
                {
                    token.ReadPacket.DstAddr = cache.DstAddr;
                    token.ReadPacket.DstPort = cache.DstPort;
                    token.IPEndPoint = new IPEndPoint(NetworkHelper.ToIP(token.ReadPacket.DstAddr), token.ReadPacket.DstPort);

                    cache.Connection = await ConnectTunnel(cache.MachineId, []).ConfigureAwait(false);
                    token.Connection = cache.Connection;
                }
            }
            else if (token.Connection != null)
            {
                token.Connection = await ConnectTunnel(token.Connection.RemoteMachineId, []).ConfigureAwait(false);
            }

            return 0;
        }

        public void StartForward(IPEndPoint ep, IPEndPoint target, string machineId, byte bufferSize)
        {
            StopForward(ep.Port);
            Start(ep, bufferSize);

            ForwardProxyCacheInfo cache = new ForwardProxyCacheInfo
            {
                Port = LocalEndpoint.Port,
                DstAddr = NetworkHelper.ToValue(target.Address),
                DstPort = (ushort)target.Port,
                MachineId = machineId,

            };
            caches.AddOrUpdate(LocalEndpoint.Port, cache, (a, b) => cache);
            Version.Increment();
        }

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
