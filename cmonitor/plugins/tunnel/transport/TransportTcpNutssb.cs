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
            TunnelTransportState state = await ConnectForward(tunnelTransportInfo.Local, tunnelTransportInfo.Remote, tunnelTransportInfo);
            if (state != null)
            {
                //OnConnected(state);
                return state;
            }

            TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
            tunnelTransportInfo1.Direction = TunnelTransportDirection.Reverse;
            BindAndTTL(tunnelTransportInfo1.Local, tunnelTransportInfo1.Remote, tunnelTransportInfo1);
            _ = OnSendConnectBegin(tunnelTransportInfo1);

            state = await WaitReverse(tunnelTransportInfo1);
            if (state != null)
            {
                //OnConnected(state);
                return state;
            }

            await OnSendConnectFail(tunnelTransportInfo);
            OnConnectFail(tunnelTransportInfo1.Remote.MachineName);
            return null;
        }

        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            OnConnectBegin(tunnelTransportInfo);
            Task.Run(async () =>
            {
                if (tunnelTransportInfo.Direction == TunnelTransportDirection.Forward)
                {
                    BindAndTTL(tunnelTransportInfo.Remote, tunnelTransportInfo.Local, tunnelTransportInfo);
                }
                else
                {
                    TunnelTransportState state = await ConnectForward(tunnelTransportInfo.Remote, tunnelTransportInfo.Local, tunnelTransportInfo);
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
            tunnelBindServer.RemoveBind(tunnelTransportInfo.BindEP.Port);
        }

        private async Task<TunnelTransportState> ConnectForward(TunnelTransportExternalIPInfo local, TunnelTransportExternalIPInfo remote, TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(remote.Local.Address,remote.Local.Port),
                new IPEndPoint(remote.Local.Address,remote.Remote.Port),
                new IPEndPoint(remote.Local.Address,remote.Remote.Port+1),
                new IPEndPoint(remote.Remote.Address,remote.Remote.Port),
                new IPEndPoint(remote.Remote.Address,remote.Remote.Port+1),
            };
            //过滤掉不支持IPV6的情况，去尝试连接
            IEnumerable<IAsyncResult> results = eps.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
            {
                using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, local.Local.Port));
                IAsyncResult result = targetSocket.BeginConnect(ip, null, targetSocket);
                return result;
            });
            //检查一下是否连接成功
            for (int i = 0; i < 10; i++)
            {
                //全部完成，但是没有连接成功的
                if (results.Count(c => c.IsCompleted && (c.AsyncState as Socket).Connected == false) == results.Count())
                {
                    return null;
                }

                IAsyncResult result = results.FirstOrDefault(c => c.IsCompleted && (c.AsyncState as Socket).Connected);
                if (result != null)
                {
                    return new TunnelTransportState
                    {
                        ConnectedObject = result.AsyncState as Socket,
                        TransactionId = tunnelTransportInfo.TransactionId,
                        RemoteMachineName = remote.MachineName,
                        TransportName = Name,
                        TransportType = Type
                    };
                }
                await Task.Delay(10);
            }
            return null;
        }
        private void BindAndTTL(TunnelTransportExternalIPInfo local, TunnelTransportExternalIPInfo remote, TunnelTransportInfo tunnelTransportInfo)
        {
            tunnelBindServer.Bind(local.Local, tunnelTransportInfo);

            //给对方发送TTL消息
            IPEndPoint[] eps = new IPEndPoint[] {
                new IPEndPoint(remote.Local.Address,remote.Local.Port),
                new IPEndPoint(remote.Local.Address,remote.Remote.Port),
                new IPEndPoint(remote.Local.Address,remote.Remote.Port+1),
                new IPEndPoint(remote.Remote.Address,remote.Remote.Port),
                new IPEndPoint(remote.Remote.Address,remote.Remote.Port+1),
            };
            //过滤掉不支持IPV6的情况
            IEnumerable<Socket> sockets = eps.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
            {
                using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                    targetSocket.Ttl = (short)(local.RouteLevel);
                    targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, local.Local.Port));
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
                    RemoteMachineName = _state.FromMachineName,
                    TransportType = ProtocolType.Tcp,
                    ConnectedObject = socket,
                    TransactionId = _state.TransactionId,
                    TransportName = _state.TransportName,
                };

                if (reverseDic.TryRemove(_state.FromMachineName, out TaskCompletionSource<TunnelTransportState> tcs))
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
                    RemoteMachineName = _state.FromMachineName,
                    TransportType = ProtocolType.Tcp,
                    TransactionId = _state.TransactionId,
                    TransportName = _state.TransportName,
                };
                OnConnected(result);
            }
        }

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false);
        }

    }
}
