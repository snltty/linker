using cmonitor.tunnel.adapter;
using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace cmonitor.tunnel.transport
{
    public sealed class TunnelTransportTcpNutssb : ITunnelTransport
    {
        public string Name => "TcpNutssb";
        public string Label => "TCP、低TTL";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        /// <summary>
        /// 发送开始连接消息
        /// </summary>
        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        /// <summary>
        /// 发送连接失败消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        /// <summary>
        /// 发送连接成功消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

        private readonly ITunnelAdapter tunnelAdapter;
        public TunnelTransportTcpNutssb(ITunnelAdapter tunnelAdapter)
        {
            this.tunnelAdapter = tunnelAdapter;
        }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                //正向连接
                if (await OnSendConnectBegin(tunnelTransportInfo) == false)
                {
                    return null;
                }
                await Task.Delay(500);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                _ = StartListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
                BindAndTTL(tunnelTransportInfo1);
                if (await OnSendConnectBegin(tunnelTransportInfo1) == false)
                {
                    return null;
                }
                //等待对方连接，如果连接成功，我会收到一个socket，并且创建一个连接对象，失败的话会超时，那就是null
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo);
                    return connection;
                }
            }


            await OnSendConnectFail(tunnelTransportInfo);
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && tunnelAdapter.Certificate == null)
            {
                Logger.Instance.Error($"{Name}->ssl Certificate not found");
                _ = OnSendConnectSuccess(tunnelTransportInfo);
                return;
            }
            //正向连接，也就是它要连接我，那我就监听
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                _ = StartListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
            }
            Task.Run(async () =>
            {
                //正向连接，也就是它要连接我，那我就给它发TTL消息
                if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
                {
                    BindAndTTL(tunnelTransportInfo);
                }
                //我要连它，那就连接
                else
                {
                    ITunnelConnection connection = await ConnectForward(tunnelTransportInfo);
                    if (connection != null)
                    {
                        OnConnected(connection);
                        await OnSendConnectSuccess(tunnelTransportInfo);
                    }
                    else
                    {
                        await OnSendConnectFail(tunnelTransportInfo);
                    }
                }
            });
        }

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }

        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            //先尝试内网ipv4
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //在尝试外网
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //再尝试IPV6
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) || c.AddressFamily == AddressFamily.InterNetwork)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", eps.Select(c => c.ToString()))}");
            }

            foreach (IPEndPoint ep in eps)
            {
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {

                    targetSocket.IPv6Only(ep.Address.AddressFamily, false);
                    targetSocket.KeepAlive();
                    targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    await targetSocket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(ep.Address.Equals(tunnelTransportInfo.Remote.Remote.Address) ? 500 : 100));

                    //需要ssl
                    SslStream sslStream = null;
                    if (tunnelTransportInfo.SSL)
                    {
                        sslStream = new SslStream(new NetworkStream(targetSocket, false), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 });
                    }

                    return new TunnelConnectionTcp
                    {
                        Stream = sslStream,
                        Socket = targetSocket,
                        IPEndPoint = targetSocket.RemoteEndPoint as IPEndPoint,
                        TransactionId = tunnelTransportInfo.TransactionId,
                        RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                         RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        TransportName = Name,
                        Direction = tunnelTransportInfo.Direction,
                        ProtocolType = ProtocolType,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Client,
                        Label = string.Empty,
                        SSL = tunnelTransportInfo.SSL
                    };
                }
                catch (Exception)
                {
                    targetSocket.SafeClose();
                }
            }
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            //给对方发送TTL消息
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            foreach (IPAddress item in localIps)
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) || c.AddressFamily == AddressFamily.InterNetwork)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            //过滤掉不支持IPV6的情况
            IEnumerable<Socket> sockets = eps.Select(ip =>
            {
                Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                    targetSocket.Ttl = ip.Address.AddressFamily == AddressFamily.InterNetworkV6 ? (short)2 : (short)(tunnelTransportInfo.Local.RouteLevel);
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


        private ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>>();
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>();
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000));
                return connection;
            }
            catch (Exception)
            {
            }
            finally
            {
                reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
            }
            return null;
        }

        private async Task OnTcpConnected(object state, Socket socket)
        {
            if (state is TunnelTransportInfo _state && _state.TransportName == Name)
            {
                try
                {
                    SslStream sslStream = null;
                    if (_state.SSL)
                    {
                        if (tunnelAdapter.Certificate == null)
                        {
                            Logger.Instance.Error($"{Name}-> ssl Certificate not found");
                            socket.SafeClose();
                            return;
                        }

                        sslStream = new SslStream(new NetworkStream(socket, false), false);
                        await sslStream.AuthenticateAsServerAsync(tunnelAdapter.Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    }

                    TunnelConnectionTcp result = new TunnelConnectionTcp
                    {
                        RemoteMachineId = _state.Remote.MachineId,
                         RemoteMachineName= _state.Remote.MachineName,
                        Direction = _state.Direction,
                        ProtocolType = TunnelProtocolType.Tcp,
                        Stream = sslStream,
                        Socket = socket,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Server,
                        TransactionId = _state.TransactionId,
                        TransportName = _state.TransportName,
                        IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
                        Label = string.Empty,
                        SSL = _state.SSL
                    };
                    if (reverseDic.TryRemove(_state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
                    {
                        tcs.SetResult(result);
                        return;
                    }

                    OnConnected(result);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }
        }


        private async Task StartListen(IPEndPoint local, TunnelTransportInfo tunnelTransportInfo)
        {
            IPAddress localIP = NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;
            Socket socket = new Socket(localIP.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            //socket.ReceiveBufferSize = 128 * 1024;
            socket.IPv6Only(localIP.AddressFamily, false);
            socket.ReuseBind(new IPEndPoint(localIP, local.Port));
            socket.Listen(int.MaxValue);

            try
            {
                Socket client = await socket.AcceptAsync().WaitAsync(TimeSpan.FromMilliseconds(30000));
                await OnTcpConnected(tunnelTransportInfo, client);
            }
            catch (Exception)
            {
            }
            try
            {
                socket.SafeClose();
            }
            catch (Exception)
            {
            }
        }
    }
}
