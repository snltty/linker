
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.tunnel.wanport;

namespace linker.tunnel.transport
{
    /// <summary>
    /// TCP打洞
    /// 
    ///  大致原理（正向打洞）
    ///  A 通知 B，我要连你
    ///  B 收到通知，开始监听连接，并且以低TTL方式尝试连接A，这时候A肯定收不到
    ///  A 正常去连接 B，能连接成功则通道可用
    /// </summary>
    public sealed class TransportTcpNutssb : ITunnelTransport
    {
        public string Name => "TcpNutssb";
        public string Label => "TCP、低TTL";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public TunnelType TunnelType => TunnelType.P2P;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 6;

        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportTcpNutssb(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        private X509Certificate certificate;
        public void SetSSL(X509Certificate certificate)
        {
            this.certificate = certificate;
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
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(1000).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                _ = StartListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
                BindAndTTL(tunnelTransportInfo1);
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                //等待对方连接，如果连接成功，我会收到一个socket，并且创建一个连接对象，失败的话会超时，那就是null
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }


            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
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
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection);
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                }
                else
                {
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                }
            }
        }

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.TrySetResult(null);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.TrySetResult(null);
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
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(ep.Address.Equals(tunnelTransportInfo.Remote.Remote.Address) ? 500 : 100));
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {

                    targetSocket.KeepAlive();
                    targetSocket.IPv6Only(ep.AddressFamily, false);
                    targetSocket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    await targetSocket.ConnectAsync(ep, cts.Token).ConfigureAwait(false);

                    //需要ssl
                    SslStream sslStream = null;
                    if (tunnelTransportInfo.SSL)
                    {
                        sslStream = new SslStream(new NetworkStream(targetSocket, false), false, ValidateServerCertificate, null);
                        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                            ClientCertificates = new X509CertificateCollection { certificate }
                        }).ConfigureAwait(false);
                    }

                    return new TunnelConnectionTcp
                    {
                        Stream = sslStream,
                        Socket = targetSocket,
                        IPEndPoint = (targetSocket.RemoteEndPoint as IPEndPoint).MapToIPv4(),
                        TransactionId = tunnelTransportInfo.TransactionId,
                        TransactionTag = tunnelTransportInfo.TransactionTag,
                        RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                        RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        TransportName = Name,
                        Direction = tunnelTransportInfo.Direction,
                        ProtocolType = ProtocolType,
                        Type = TunnelType,
                        Mode = TunnelMode.Client,
                        Label = string.Empty,
                        SSL = tunnelTransportInfo.SSL,
                        BufferSize = tunnelTransportInfo.BufferSize,
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
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Info($"【P2P {Name}】Certificate validation: {certificate?.Subject}");
                LoggerHelper.Instance.Info($"【P2P {Name}】SSL Policy Errors: {sslPolicyErrors}");
            }
            return true;
        }
        private static void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            IPEndPoint local = new IPEndPoint(IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port);

            foreach (var ip in tunnelTransportInfo.RemoteEndPoints)
            {
                Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {
                    targetSocket.IPv6Only(ip.AddressFamily, false);
                    targetSocket.ReuseBind(local);
                    targetSocket.Ttl = (short)(tunnelTransportInfo.Local.RouteLevel);
                    _ = targetSocket.ConnectAsync(ip);
                    targetSocket.SafeClose();
                }
                catch (Exception)
                {
                }
            }
        }


        private ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>>();
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.WithTimeout(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                return connection;
            }
            catch (Exception)
            {
                tcs.TrySetResult(null);
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
                SslStream sslStream = null;
                socket.KeepAlive();
                try
                {

                    if (_state.SSL)
                    {
                        if (certificate == null)
                        {
                            LoggerHelper.Instance.Error($"{Name}-> ssl Certificate not found");
                            socket.SafeClose();
                            return;
                        }

                        sslStream = new SslStream(new NetworkStream(socket, false), false, ValidateServerCertificate, null);
                        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
                        await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                        {
                            ServerCertificate = certificate,
                            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                            ClientCertificateRequired = OperatingSystem.IsAndroid(),
                        }, cts.Token).ConfigureAwait(false);
                    }

                    TunnelConnectionTcp result = new TunnelConnectionTcp
                    {
                        RemoteMachineId = _state.Remote.MachineId,
                        RemoteMachineName = _state.Remote.MachineName,
                        Direction = _state.Direction,
                        ProtocolType = TunnelProtocolType.Tcp,
                        Stream = sslStream,
                        Socket = socket,
                        Type = TunnelType,
                        Mode = TunnelMode.Server,
                        TransactionId = _state.TransactionId,
                        TransactionTag = _state.TransactionTag,
                        TransportName = _state.TransportName,
                        IPEndPoint = (socket.RemoteEndPoint as IPEndPoint).MapToIPv4(),
                        Label = string.Empty,
                        SSL = _state.SSL,
                        BufferSize = _state.BufferSize,
                    };
                    if (reverseDic.TryRemove(_state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
                    {
                        tcs.TrySetResult(result);
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
                    socket?.SafeClose();
                    sslStream?.Dispose();
                }
            }
        }


        private async Task StartListen(IPEndPoint local, TunnelTransportInfo tunnelTransportInfo)
        {
            IPAddress localIP = IPAddress.IPv6Any;
            Socket socket = new Socket(localIP.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            socket.IPv6Only(localIP.AddressFamily, false);
            socket.ReuseBind(new IPEndPoint(localIP, local.Port));
            socket.Listen(int.MaxValue);

            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                Socket client = await socket.AcceptAsync(cts.Token).ConfigureAwait(false);
                await OnTcpConnected(tunnelTransportInfo, client).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            socket.SafeClose();
        }
    }
}
