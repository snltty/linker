using cmonitor.client.tunnel;
using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace cmonitor.plugins.tunnel.transport
{
    public sealed class TransportMsQuic : ITunnelTransport
    {
        public string Name => "msquic";

        public string Label => "UDP over MsQuic，win11+、linux";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Quic;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private ConcurrentDictionary<int, ListenAsyncToken> stateDic = new ConcurrentDictionary<int, ListenAsyncToken>();
        private byte[] authBytes = Encoding.UTF8.GetBytes("snltty.ttl");
        private byte[] endBytes = Encoding.UTF8.GetBytes("snltty.end");
        private IPEndPoint quicListenEP = null;

        private X509Certificate serverCertificate;
        public TransportMsQuic(Config config)
        {
            string path = Path.GetFullPath(config.Data.Client.Tunnel.Certificate);
            if (File.Exists(path))
            {
                serverCertificate = new X509Certificate(path, config.Data.Client.Tunnel.Password);
            }
            else
            {
                Logger.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
            _ = QuicStart();
        }

        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    await OnSendConnectFail(tunnelTransportInfo);
                    return null;
                }
            }

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
                _ = BindListen(tunnelTransportInfo1.Local.Local, quicListenEP, tunnelTransportInfo1);
                await Task.Delay(50);
                BindAndTTL(tunnelTransportInfo1);
                if (await OnSendConnectBegin(tunnelTransportInfo1) == false)
                {
                    return null;
                }
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
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    OnSendConnectFail(tunnelTransportInfo);
                    return;
                }
            }
            Task.Run(async () =>
            {
                if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
                {
                    _ = BindListen(tunnelTransportInfo.Local.Local, quicListenEP, tunnelTransportInfo);
                    await Task.Delay(50);
                    BindAndTTL(tunnelTransportInfo);
                }
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

        private (UdpClient, UdpClient) BindListen(IPEndPoint local, TaskCompletionSource<IPEndPoint> tcs)
        {
            UdpClient udpClient = new UdpClient(local.AddressFamily);
            udpClient.Client.ReuseBind(local);
            udpClient.Client.WindowsUdpBug();
            IAsyncResult result = udpClient.BeginReceive((IAsyncResult result) =>
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = udpClient.EndReceive(result, ref ep);
                    udpClient.Send(endBytes, ep);
                    tcs.SetResult(ep);
                }
                catch (Exception)
                {
                }
            }, null);

            UdpClient udpClient6 = new UdpClient(AddressFamily.InterNetworkV6);
            udpClient6.Client.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
            udpClient6.Client.WindowsUdpBug();
            IAsyncResult result6 = udpClient6.BeginReceive((IAsyncResult result) =>
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = udpClient6.EndReceive(result, ref ep);
                    udpClient6.Send(endBytes, ep);
                    tcs.SetResult(ep);
                }
                catch (Exception)
                {
                }
            }, null);


            return (udpClient, udpClient6);
        }
        private UdpClient BindListen(UdpClient remoteUdp, IPEndPoint remoteEP)
        {
            UdpClient localUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            localUdp.Client.WindowsUdpBug();

            ListenAsyncToken token = new ListenAsyncToken
            {
                LocalUdp = remoteUdp,
                Received = false,
                RemoteUdp = localUdp,
            };
            remoteUdp.BeginReceive(ListenConnectCallback, token);

            localUdp.BeginReceive(ListenConnectCallback, new ListenAsyncToken
            {
                LocalUdp = localUdp,
                Received = false,
                RemoteUdp = remoteUdp,
                RemoteEP = remoteEP,
                State = token
            });

            return localUdp;
        }
        private void ListenConnectCallback(IAsyncResult result)
        {
            ListenAsyncToken token = result.AsyncState as ListenAsyncToken;
            try
            {
                byte[] bytes = token.LocalUdp.EndReceive(result, ref token.tempEP);
                if (token.Received == false)
                {
                    if (token.State is ListenAsyncToken targetToken)
                    {
                        targetToken.RemoteEP = token.tempEP;
                    }
                }
                token.Received = true;

                token.RemoteUdp.Send(bytes, token.RemoteEP);

                token.LocalUdp.BeginReceive(ListenConnectCallback, token);

            }
            catch (Exception)
            {
                try
                {
                    token.LocalUdp.Close();
                    token.RemoteUdp.Close();
                }
                catch (Exception)
                {
                }
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

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", eps.Select(c => c.ToString()))}");
            }

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>();
            //接收远端数据，收到了就是成功了
            (UdpClient remoteUdp, UdpClient remoteUdp6) = BindListen(local, taskCompletionSource);

            foreach (IPEndPoint ep in eps.Where(c => NetworkHelper.NotIPv6Support(c.Address) == false))
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    if (ep.AddressFamily == AddressFamily.InterNetwork)
                    {
                        remoteUdp.Send(authBytes, ep);
                    }
                    else
                    {
                        remoteUdp6.Send(authBytes, ep);
                    }
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex.Message);
                    }
                }
            }

            try
            {
                IPEndPoint remoteEP = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
                //绑定一个udp，用来给QUIC链接
                UdpClient localUdp = BindListen(remoteEP.AddressFamily == AddressFamily.InterNetwork ? remoteUdp : remoteUdp6, remoteEP);

                QuicConnection connection = connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, (localUdp.Client.LocalEndPoint as IPEndPoint).Port),
                    LocalEndPoint = new IPEndPoint(IPAddress.Any, 0),
                    DefaultCloseErrorCode = 0x0a,
                    DefaultStreamErrorCode = 0x0b,
                    IdleTimeout = TimeSpan.FromMilliseconds(15000),
                    ClientAuthenticationOptions = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                        EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13,
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                        {
                            return true;
                        }
                    }
                }).AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000));
                QuicStream quicStream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                return new TunnelConnectionMsQuic
                {
                    LocalUdp = localUdp,
                    remoteUdp = remoteUdp,
                    Stream = quicStream,
                    Connection = connection,
                    IPEndPoint = remoteEP,
                    TransactionId = tunnelTransportInfo.TransactionId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    TransportName = Name,
                    Direction = tunnelTransportInfo.Direction,
                    ProtocolType = ProtocolType,
                    Type = TunnelType.P2P,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                };
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            try
            {
                remoteUdp.Close();
            }
            catch (Exception)
            {
            }
            return null;
        }


        private async Task BindListen(IPEndPoint local, IPEndPoint targetEP, TunnelTransportInfo state)
        {
            UdpClient udpClient = new UdpClient(local.AddressFamily);
            UdpClient udpClient6 = new UdpClient(AddressFamily.InterNetworkV6);

            try
            {
                udpClient.Client.ReuseBind(local);
                udpClient.Client.WindowsUdpBug();
                ListenAsyncToken token = new ListenAsyncToken
                {
                    Step = ListenStep.Auth,
                    LocalUdp = udpClient,
                    RemoteEP = targetEP,
                    Tcs = new TaskCompletionSource<bool>(),
                    State = state
                };
                udpClient.BeginReceive(ListenReceiveCallback, token);


                udpClient6.Client.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                udpClient6.Client.WindowsUdpBug();
                ListenAsyncToken token6 = new ListenAsyncToken
                {
                    Step = ListenStep.Auth,
                    LocalUdp = udpClient6,
                    RemoteEP = targetEP,
                    Tcs = token.Tcs,
                    State = state
                };
                udpClient6.BeginReceive(ListenReceiveCallback, token6);

                bool result = await token.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(30000));
            }
            catch (Exception ex)
            {
                udpClient.Close();
                udpClient6.Close();
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
        private void ListenReceiveCallback(IAsyncResult result)
        {
            ListenAsyncToken token = result.AsyncState as ListenAsyncToken;
            try
            {

                byte[] bytes = token.LocalUdp.EndReceive(result, ref token.tempEP);
                if (token.Step == ListenStep.Auth)
                {
                    if (bytes.Length == endBytes.Length && bytes.AsSpan().SequenceEqual(endBytes))
                    {
                        token.Step = ListenStep.Forward;
                    }
                    else
                    {
                        token.LocalUdp.Send(bytes, token.tempEP);
                    }
                    if (token.Tcs != null && token.Tcs.Task.IsCompleted == false)
                    {
                        token.Tcs.SetResult(true);
                    }
                }
                else
                {
                    if (token.RemoteUdp == null)
                    {
                        token.RemoteUdp = new UdpClient();
                        token.RemoteUdp.Client.WindowsUdpBug();
                    }
                    if (token.Received == false)
                    {
                        token.RemoteUdp.Send(bytes, token.RemoteEP);
                        token.Received = true;
                        token.RealRemoteEP = token.tempEP;
                        stateDic.AddOrUpdate((token.RemoteUdp.Client.LocalEndPoint as IPEndPoint).Port, token, (a, b) => token);
                        token.RemoteUdp.BeginReceive(ListenReceiveCallback, new ListenAsyncToken
                        {
                            LocalUdp = token.RemoteUdp,
                            Step = ListenStep.Forward,
                            RemoteEP = token.tempEP,
                            RemoteUdp = token.LocalUdp,
                            RealRemoteEP = token.tempEP,
                            Received = true,
                        }) ;
                    }
                    else
                    {
                        token.RemoteUdp.Send(bytes, token.RemoteEP);
                    }
                }
                token.LocalUdp.BeginReceive(ListenReceiveCallback, token);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                try
                {
                    stateDic.TryRemove((token.RemoteUdp.Client.LocalEndPoint as IPEndPoint).Port, out _);
                    token.LocalUdp?.Close();
                    token.RemoteUdp?.Close();
                }
                catch (Exception)
                {
                }
            }
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

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in eps.Where(c => NetworkHelper.NotIPv6Support(c.Address) == false))
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineName} {ip}");
                    }

                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();
                        socket.ReuseBind(local);
                        socket.Ttl = (short)(tunnelTransportInfo.Local.RouteLevel);
                        _ = socket.SendToAsync(new byte[0], SocketFlags.None, ip);
                        socket.SafeClose();
                    }
                    else
                    {
                        Socket socket = new Socket(ip.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();
                        socket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                        socket.Ttl = 2;
                        _ = socket.SendToAsync(new byte[0], SocketFlags.None, ip);
                        socket.SafeClose();
                    }
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex.Message);
                    }
                }
                finally
                {
                }
            }
        }


        private ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>>();
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>();
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineName, tcs);

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
                reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out _);
            }
            return null;
        }
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }


        private async Task OnUdpConnected(object _state, UdpClient localUdp, UdpClient remoteUdp,IPEndPoint remoteEP, QuicConnection quicConnection, QuicStream stream)
        {
            TunnelTransportInfo state = _state as TunnelTransportInfo;
            if (state.TransportName == Name)
            {
                try
                {
                    TunnelConnectionMsQuic result = new TunnelConnectionMsQuic
                    {
                        LocalUdp = localUdp,
                        remoteUdp = remoteUdp,
                        RemoteMachineName = state.Remote.MachineName,
                        Direction = state.Direction,
                        ProtocolType = TunnelProtocolType.Quic,
                        Stream = stream,
                        Connection = quicConnection,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Server,
                        TransactionId = state.TransactionId,
                        TransportName = state.TransportName,
                        IPEndPoint = remoteEP,
                        Label = string.Empty,
                    };
                    if (reverseDic.TryRemove(state.Remote.MachineName, out TaskCompletionSource<ITunnelConnection> tcs))
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
            await Task.CompletedTask;
        }

        private async Task QuicStart()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false) return;

                QuicListener listener = await QuicListener.ListenAsync(new QuicListenerOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                    ListenBacklog = int.MaxValue,
                    ListenEndPoint = new IPEndPoint(IPAddress.Any, 0),
                    ConnectionOptionsCallback = (connection, hello, token) =>
                    {
                        return ValueTask.FromResult(new QuicServerConnectionOptions
                        {
                            MaxInboundBidirectionalStreams = 65535,
                            MaxInboundUnidirectionalStreams = 65535,
                            DefaultCloseErrorCode = 0x0a,
                            DefaultStreamErrorCode = 0x0b,
                            IdleTimeout = TimeSpan.FromMilliseconds(15000),
                            ServerAuthenticationOptions = new SslServerAuthenticationOptions
                            {
                                ServerCertificate = serverCertificate,
                                EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13,
                                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
                            }
                        });
                    }
                });
                quicListenEP = new IPEndPoint(IPAddress.Loopback, listener.LocalEndPoint.Port);
                while (true)
                {
                    try
                    {
                        QuicConnection quicConnection = await listener.AcceptConnectionAsync();
                        QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync();

                        if (stateDic.TryRemove(quicConnection.RemoteEndPoint.Port, out ListenAsyncToken token))
                        {
                            await OnUdpConnected(token.State, token.LocalUdp, token.RemoteUdp, token.RealRemoteEP, quicConnection, quicStream);
                        }
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
        }


        sealed class ListenAsyncToken
        {
            public ListenStep Step { get; set; }
            public UdpClient LocalUdp { get; set; }
            public IPEndPoint tempEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

            public UdpClient RemoteUdp { get; set; }
            public IPEndPoint RemoteEP { get; set; }
            public IPEndPoint RealRemoteEP { get; set; }

            public TaskCompletionSource<bool> Tcs { get; set; }

            public object State { get; set; }

            public bool Received { get; set; }
        }

        enum ListenStep : byte
        {
            Auth = 0,
            Forward = 1
        }
    }


}
