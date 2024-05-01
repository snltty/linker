using cmonitor.plugins.tunnel.server;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.transport
{
    public sealed class TransportTcpNutssb : ITransport
    {
        public string Name => "TcpNutssb";
        public ProtocolType Type => ProtocolType.Tcp;

        public Func<TunnelTransportInfo, Task> OnSendConnectBegin { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<TunnelTransportInfo> OnConnectBegin { get; set; } = (info) => { };
        public Action<TunnelTransportInfo> OnConnecting { get; set; }
        public Action<TunnelTransportState> OnConnected { get; set; } = (state) => { };
        public Action<TunnelTransportState> OnDisConnected { get; set; } = (state) => { };
        public Action<string> OnConnectFail { get; set; } = (machineName) => { };


        private readonly TunnelBindServer tunnelBindServer;

        public TransportTcpNutssb(TunnelBindServer tunnelBindServer)
        {
            this.tunnelBindServer = tunnelBindServer;
            tunnelBindServer.OnTcpConnected += OnTcpConnected;
            tunnelBindServer.OnDisConnected += OnTcpDisConnected;
        }

        public async Task<TunnelTransportState> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            OnConnecting(tunnelTransportInfo);

            tunnelTransportInfo.Direction = TunnelTransportDirection.Forward;
            await OnSendConnectBegin(tunnelTransportInfo);
            TunnelTransportState state = await ConnectForward(tunnelTransportInfo);
            if (state != null)
            {
                return state;
            }

            TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
            tunnelTransportInfo1.Direction = TunnelTransportDirection.Reverse;
            tunnelBindServer.Bind(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
            BindAndTTL(tunnelTransportInfo1);
            await OnSendConnectBegin(tunnelTransportInfo1);

            state = await WaitReverse(tunnelTransportInfo1);
            if (state != null)
            {
                return state;
            }

            await OnSendConnectFail(tunnelTransportInfo);
            OnConnectFail(tunnelTransportInfo.Remote.MachineName);
            return null;
        }

        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            OnConnectBegin(tunnelTransportInfo);
            if (tunnelTransportInfo.Direction == TunnelTransportDirection.Forward)
            {
                tunnelBindServer.Bind(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
            }
            Task.Run(async () =>
            {
                if (tunnelTransportInfo.Direction == TunnelTransportDirection.Forward)
                {
                    BindAndTTL(tunnelTransportInfo);
                }
                else
                {
                    TunnelTransportState state = await ConnectForward(tunnelTransportInfo);
                    if (state != null)
                    {
                        OnConnected(state);
                    }
                    else
                    {
                        await OnSendConnectFail(tunnelTransportInfo);
                        OnConnectFail(tunnelTransportInfo.Local.MachineName);
                    }
                }
            });
        }

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            tunnelBindServer.RemoveBind(tunnelTransportInfo.Local.Local.Port);
        }

        private async Task<TunnelTransportState> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            await Task.Delay(20);
            //要连接哪些IP
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Remote.Local.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Remote.Remote.Port+1),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            };
            foreach (IPEndPoint ep in eps)
            {
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.IPv6Only(ep.Address.AddressFamily, false);
                targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                IAsyncResult result = targetSocket.BeginConnect(ep, null, null);

                for (int i = 0; i < 25; i++)
                {
                    if (result.IsCompleted)
                    {
                        break;
                    }
                    await Task.Delay(20);
                }
                try
                {
                    if (result.IsCompleted == false)
                    {
                        targetSocket.SafeClose();
                        continue;
                    }

                    targetSocket.EndConnect(result);
                    return new TunnelTransportState
                    {
                        ConnectedObject = targetSocket,
                        TransactionId = tunnelTransportInfo.TransactionId,
                        RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        TransportName = Name,
                        TransportType = Type
                    };
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(targetSocket.RemoteEndPoint.ToString());
                        Logger.Instance.Error(ex);
                    }
                    targetSocket.SafeClose();
                }
            }
            return null;
        }
        private void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            //给对方发送TTL消息
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Local.Local.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Local.Address,tunnelTransportInfo.Remote.Remote.Port+1),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            };
            //过滤掉不支持IPV6的情况
            IEnumerable<Socket> sockets = eps.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
            {
                Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                    targetSocket.Ttl = (short)(tunnelTransportInfo.Local.RouteLevel + 1);
                    targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));
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
        }


        private ConcurrentDictionary<string, TaskCompletionSource<TunnelTransportState>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<TunnelTransportState>>();
        private async Task<TunnelTransportState> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<TunnelTransportState> tcs = new TaskCompletionSource<TunnelTransportState>();
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineName, tcs);

            TunnelTransportState state = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

            reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out _);

            return state;
        }


        private void OnTcpConnected(object state, Socket socket)
        {
            if (state is TunnelTransportInfo _state && _state.TransportName == Name)
            {
                TunnelTransportState result = new TunnelTransportState
                {
                    RemoteMachineName = _state.Remote.MachineName,
                    TransportType = ProtocolType.Tcp,
                    ConnectedObject = socket,
                    TransactionId = _state.TransactionId,
                    TransportName = _state.TransportName,
                };
                if (reverseDic.TryRemove(_state.Remote.MachineName, out TaskCompletionSource<TunnelTransportState> tcs))
                {
                    tcs.SetResult(result);
                    return;
                }

                OnConnected(result);
            }
        }
        private void OnTcpDisConnected(object state)
        {
            if (state is TunnelTransportInfo _state && _state.TransportName == Name)
            {
                TunnelTransportState result = new TunnelTransportState
                {
                    RemoteMachineName = _state.Remote.MachineName,
                    TransportType = ProtocolType.Tcp,
                    TransactionId = _state.TransactionId,
                    TransportName = _state.TransportName,
                };
                OnDisConnected(result);
            }
        }

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false);
        }

        public sealed class ConnectResultInfo
        {
            public IAsyncResult Result { get; set; }
            public Socket Socket { get; set; }
            public bool EndConnected { get; set; }
        }
    }
}
