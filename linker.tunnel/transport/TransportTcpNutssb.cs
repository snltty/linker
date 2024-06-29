using linker.tunnel.adapter;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.transport
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
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && tunnelAdapter.Certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await OnSendConnectSuccess(tunnelTransportInfo);
                return;
            }
            //正向连接，也就是它要连接我，那我就监听
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                _ = StartListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
            }
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
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            foreach (IPEndPoint ep in tunnelTransportInfo.RemoteEndPoints)
            {
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {

                    targetSocket.IPv6Only(ep.Address.AddressFamily, false);
                    targetSocket.KeepAlive();
                    targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
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
            IEnumerable<Socket> sockets = tunnelTransportInfo.RemoteEndPoints.Select(ip =>
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
                    socket.KeepAlive();
                    SslStream sslStream = null;
                    if (_state.SSL)
                    {
                        if (tunnelAdapter.Certificate == null)
                        {
                            LoggerHelper.Instance.Error($"{Name}-> ssl Certificate not found");
                            socket.SafeClose();
                            return;
                        }

                        sslStream = new SslStream(new NetworkStream(socket, false), false);
                        await sslStream.AuthenticateAsServerAsync(tunnelAdapter.Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false);
                    }

                    TunnelConnectionTcp result = new TunnelConnectionTcp
                    {
                        RemoteMachineId = _state.Remote.MachineId,
                        RemoteMachineName = _state.Remote.MachineName,
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
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }
        }


        private async Task StartListen(IPEndPoint local, TunnelTransportInfo tunnelTransportInfo)
        {
            IPAddress localIP = NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;
            Socket socket = new Socket(localIP.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            //socket.ReceiveBufferSize = 5 * 1024 * 1024;

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
