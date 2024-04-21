using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.server;
using common.libs;
using common.libs.extends;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.transport
{
    public sealed class TransportTcpNutssb : ITransport
    {

        private readonly TunnelBindServer tunnelBindServer;
        private readonly CompactTransfer compactTransfer;

        public Action<TransportState> OnConnected { get; set; } = (state) => { };
        public Func<TunnelTransportInfo, Task<TunnelTransportInfo>> SendBegin { get; set; }
        public string Name => "TcpNutssb";
        public ProtocolType TypeFlag => ProtocolType.Tcp;

        public TransportTcpNutssb(TunnelBindServer tunnelBindServer, CompactTransfer compactTransfer)
        {
            this.tunnelBindServer = tunnelBindServer;
            tunnelBindServer.OnTcpConnected += OnTcpConnected;

            this.compactTransfer = compactTransfer;
        }

        public async Task<Socket> ConnectAsync(string fromMachineName, string toMachineName)
        {
            //获取自己的外网IP
            TunnelCompactIPEndPoint[] ips = await compactTransfer.GetExternalIPAsync(TypeFlag);
            if (ips.Length == 0)
            {
                return null;
            }

            //告诉对方，我要连你，你需要给我发送一下 低TTL，并且对方要返回它的外网地址过来，给我去连接
            TunnelTransportInfo tunnelTransportInfo = new TunnelTransportInfo
            {
                FromMachineName = fromMachineName,
                ToMachineName = toMachineName,
                FromLocal = ips[0].Local,
                FromRemote = ips[0].Remote,
            };
            TunnelTransportInfo remoteInfo = await SendBegin(tunnelTransportInfo);

            //要连接哪些IP
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(remoteInfo.FromLocal.Address,remoteInfo.FromLocal.Port),
                new IPEndPoint(remoteInfo.FromLocal.Address,remoteInfo.FromRemote.Port),
                new IPEndPoint(remoteInfo.FromLocal.Address,remoteInfo.FromRemote.Port+1),
                new IPEndPoint(remoteInfo.FromRemote.Address,remoteInfo.FromRemote.Port),
                new IPEndPoint(remoteInfo.FromRemote.Address,remoteInfo.FromRemote.Port+1),
            };
            //过滤掉不支持IPV6的情况，去尝试连接
            IEnumerable<IAsyncResult> results = eps.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
            {
                using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                    targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.FromLocal.Port));
                    IAsyncResult result = targetSocket.BeginConnect(ip, null, targetSocket);
                    return result;
                }
                catch (Exception)
                {
                }
                return null;
            });
            //检查一下是否连接成功
            for (int i = 0; i < 10; i++)
            {
                IAsyncResult result = results.FirstOrDefault(c => c.IsCompleted);
                if (result != null)
                {
                    return result.AsyncState as Socket;
                }
                await Task.Delay(10);
            }
            return null;
        }
        public async Task<TunnelTransportInfo> OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //获取自己的外网IP
            TunnelCompactIPEndPoint[] ips = await compactTransfer.GetExternalIPAsync(ProtocolType.Tcp);
            if (ips.Length == 0)
            {
                return null;
            }
            //监听，等对方来连
            TunnelTransportInfo info = new TunnelTransportInfo
            {
                ToMachineName = tunnelTransportInfo.FromMachineName,
                FromMachineName = tunnelTransportInfo.ToMachineName,
                FromLocal = ips[0].Local,
                FromRemote = ips[0].Remote,
                RouteLevel = tunnelTransportInfo.RouteLevel
            };
            TransportTcpNutssbState state = new TransportTcpNutssbState { FromMachineName = tunnelTransportInfo.FromMachineName };
            tunnelBindServer.Bind(info.FromLocal, state);

            //给对方发送TTL消息
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(tunnelTransportInfo.FromLocal.Address,tunnelTransportInfo.FromLocal.Port),
                new IPEndPoint(tunnelTransportInfo.FromLocal.Address,tunnelTransportInfo.FromRemote.Port),
                new IPEndPoint(tunnelTransportInfo.FromLocal.Address,tunnelTransportInfo.FromRemote.Port+1),
                new IPEndPoint(tunnelTransportInfo.FromRemote.Address,tunnelTransportInfo.FromRemote.Port),
                new IPEndPoint(tunnelTransportInfo.FromRemote.Address,tunnelTransportInfo.FromRemote.Port+1),
            };
            //过滤掉不支持IPV6的情况
            IEnumerable<Socket> sockets = eps.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
            {
                using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                    targetSocket.Ttl = (short)(info.RouteLevel);
                    targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, info.FromLocal.Port));
                    _ = targetSocket.ConnectAsync(ip);
                    return targetSocket;
                }
                catch (Exception)
                {
                }
                return null;
            });
            foreach (Socket item in sockets.Where(c => c != null && c.Connected == false))
            {
                item.SafeClose();
            }

            return info;
        }

        public async Task OnReverse(TunnelTransportInfo tunnelTransportInfo)
        {
        }


        private void OnTcpConnected(object state, Socket socket)
        {
            if (state is TransportTcpNutssbState _state)
            {
                OnConnected(new TransportState { FromMachineName = _state.FromMachineName, TypeFlag = ProtocolType.Tcp, ConnectedObject = socket });
            }
        }

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false);
        }

        public sealed class TransportTcpNutssbState
        {
            public string FromMachineName { get; set; }
        }
    }
}
