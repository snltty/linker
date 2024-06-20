using cmonitor.tunnel.adapter;
using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace cmonitor.tunnel.transport
{
    public sealed class TransportMsQuic : ITunnelTransport
    {
        public string Name => "msquic";

        public string Label => "MsQuic，win10+、linux";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Quic;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private ConcurrentDictionary<int, ListenAsyncToken> stateDic = new ConcurrentDictionary<int, ListenAsyncToken>();
        private byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.ttl");
        private byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.end");
        private IPEndPoint quicListenEP = null;

        private readonly ITunnelAdapter tunnelAdapter;
        public TransportMsQuic(ITunnelAdapter tunnelAdapter)
        {
            this.tunnelAdapter = tunnelAdapter;
            _ = QuicStart();

            /*
             *  QUIC监听 QuicStart
             * 
             *  大致流程
             *  
             *  1、ConnectAsync 告诉B，我要连接你
             *  
             *  2、B 收到消息调用 OnBegin，然后绑定一个 udpClientB 等待 A 的消息，
             *  3、B 给 A 发送一些消息 ，在 BindAndTTL，
             *  
             *  4、A 绑定一个监听 udpClientA，然后发消息给 B , 如果 B 收到消息，就会回一条消息，这个监听就会收到消息，在 ConnectForward
             *  5、A 再绑定一个 udpClientA1，用以接收quic的连接，
             *  
             *  6、udpClientA1 收到消息，则通过 udpClientA 发送给B， udpClientB 收到消息，创建一个udp，发送给quic监听，完成一个线路
             */
        }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    Logger.Instance.Error($"msquic not supported, need win11+,or linux");
                    await OnSendConnectFail(tunnelTransportInfo);
                    return null;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    Logger.Instance.Error($"msquic need ssl");
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
        /// <summary>
        /// 收到连接请求
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    await OnSendConnectFail(tunnelTransportInfo);
                    return;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    Logger.Instance.Error($"msquic need ssl");
                    await OnSendConnectFail(tunnelTransportInfo);
                    return;
                }
            }
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
        }
        /// <summary>
        /// 打洞连接
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
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

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>();
            //接收远端数据，收到了就是成功了
            (UdpClient remoteUdp, UdpClient remoteUdp6) = BindListen(local, taskCompletionSource);

            //给远端发送一些消息
            foreach (IPEndPoint ep in eps)
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
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
                if (remoteEP.AddressFamily == AddressFamily.InterNetwork)
                {
                    remoteUdp6.Close();
                    remoteUdp6.Dispose();
                }
                else
                {
                    remoteUdp.Close();
                    remoteUdp.Dispose();
                }

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
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
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
        /// <summary>
        /// 发送TTL
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
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

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in eps)
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ip}");
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
        /// <summary>
        /// 等待对方连接，用于反向连接的情况
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>();
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(10000));
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


        /// <summary>
        /// 绑定一个UDP，用来给对方发消息，然后接收对方返回的消息，以确定能通信
        /// </summary>
        /// <param name="local">绑定的地址</param>
        /// <param name="tcs">等待对象，等待得到对方的地址</param>
        /// <returns></returns>
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
                    //收到远端的消息，表明对方已收到，再给它发个结束消息，表示可以正常通信了
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
        /// <summary>
        /// 监听UDP，等QUIC连接
        /// </summary>
        /// <param name="remoteUdp">监听收到消息消息后，通过这个udp发送给远端</param>
        /// <param name="remoteEP">远端地址</param>
        /// <returns></returns>
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
            _ = ListenConnectCallback(token);

            _ = ListenConnectCallback(new ListenAsyncToken
            {
                LocalUdp = localUdp,
                Received = false,
                RemoteUdp = remoteUdp,
                RemoteEP = remoteEP,
                State = token
            });

            return localUdp;
        }
        private async Task ListenConnectCallback(ListenAsyncToken token)
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await token.LocalUdp.ReceiveAsync();
                    if (result.Buffer.Length == 0) break;

                    if (token.Received == false)
                    {
                        if (token.State is ListenAsyncToken targetToken)
                        {
                            targetToken.RemoteEP = result.RemoteEndPoint;
                        }
                    }
                    token.Received = true;

                    //将quic来的消息，直接发送给远端
                    token.RemoteUdp.Send(result.Buffer, token.RemoteEP);
                }

            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
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
        /// <summary>
        /// 监听UDP，等待对方发来消息，然后再回消息给它，以确定能通信
        /// </summary>
        /// <param name="local">UDP监听地址</param>
        /// <param name="quicEP">QUIC监听地址</param>
        /// <param name="state">收到连接后，调用连接成功回调，带上这个信息</param>
        /// <returns></returns>
        private async Task BindListen(IPEndPoint local, IPEndPoint quicEP, TunnelTransportInfo state)
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
                    RemoteEP = quicEP,
                    Tcs = new TaskCompletionSource<AddressFamily>(),
                    State = state
                };
                _ = ListenReceiveCallback(token);


                udpClient6.Client.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                udpClient6.Client.WindowsUdpBug();
                ListenAsyncToken token6 = new ListenAsyncToken
                {
                    Step = ListenStep.Auth,
                    LocalUdp = udpClient6,
                    RemoteEP = quicEP,
                    Tcs = token.Tcs,
                    State = state
                };
                _ = ListenReceiveCallback(token6);

                AddressFamily af = await token.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(30000));
                if (af == AddressFamily.InterNetwork)
                {
                    udpClient6.Close();
                    udpClient6.Dispose();
                }
                else
                {
                    udpClient.Close();
                    udpClient.Dispose();
                }
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
        private async Task ListenReceiveCallback(ListenAsyncToken token)
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await token.LocalUdp.ReceiveAsync();
                    if (result.Buffer.Length == 0) break;

                    //在确定通信阶段
                    if (token.Step == ListenStep.Auth)
                    {
                        //收到结束确定阶段
                        if (result.Buffer.Length == endBytes.Length && result.Buffer.AsSpan().SequenceEqual(endBytes))
                        {
                            token.Step = ListenStep.Forward;
                        }
                        else
                        {
                            //否则原样返回消息，让对方知道我收到了消息
                            token.LocalUdp.Send(result.Buffer, result.RemoteEndPoint);
                        }
                        if (token.Tcs != null && token.Tcs.Task.IsCompleted == false)
                        {
                            token.Tcs.SetResult(result.RemoteEndPoint.AddressFamily);
                        }
                    }
                    //已经确定过能通信了，可以直接往quic的监听地址发去消息
                    else
                    {
                        if (token.RemoteUdp == null)
                        {
                            token.RemoteUdp = new UdpClient();
                            token.RemoteUdp.Client.WindowsUdpBug();
                        }
                        if (token.Received == false)
                        {
                            token.RemoteUdp.Send(result.Buffer, token.RemoteEP);
                            token.Received = true;
                            token.RealRemoteEP = result.RemoteEndPoint;
                            stateDic.AddOrUpdate((token.RemoteUdp.Client.LocalEndPoint as IPEndPoint).Port, token, (a, b) => token);

                            _ = ListenReceiveCallback(new ListenAsyncToken
                            {
                                LocalUdp = token.RemoteUdp,
                                Step = ListenStep.Forward,
                                RemoteEP = result.RemoteEndPoint,
                                RemoteUdp = token.LocalUdp,
                                RealRemoteEP = result.RemoteEndPoint,
                                Received = true,
                            });
                        }
                        else
                        {
                            token.RemoteUdp.Send(result.Buffer, token.RemoteEP);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
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

        
        /// <summary>
        /// 收到连接失败
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }
        /// <summary>
        /// 收到连接成功
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        /// <param name="_state">状态信息</param>
        /// <param name="localUdp"></param>
        /// <param name="remoteUdp"></param>
        /// <param name="remoteEP"></param>
        /// <param name="quicConnection"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task OnUdpConnected(object _state, UdpClient localUdp, UdpClient remoteUdp, IPEndPoint remoteEP, QuicConnection quicConnection, QuicStream stream)
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
                        RemoteMachineId = state.Remote.MachineId,
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
                    if (reverseDic.TryRemove(state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
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

        /// <summary>
        /// QUIC监听
        /// </summary>
        /// <returns></returns>
        private async Task QuicStart()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    Logger.Instance.Error($"msquic not supported, need win11+,or linux");
                    return;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    Logger.Instance.Error($"msquic need ssl");
                    return;
                }

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
                                ServerCertificate = tunnelAdapter.Certificate,
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

                        _ = Task.Run(async () =>
                        {
                            while (true)
                            {
                                QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync();

                                if (stateDic.TryRemove(quicConnection.RemoteEndPoint.Port, out ListenAsyncToken token))
                                {
                                    await OnUdpConnected(token.State, token.LocalUdp, token.RemoteUdp, token.RealRemoteEP, quicConnection, quicStream);
                                }
                            }
                        });
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

            public UdpClient RemoteUdp { get; set; }
            public IPEndPoint RemoteEP { get; set; }
            public IPEndPoint RealRemoteEP { get; set; }

            public TaskCompletionSource<AddressFamily> Tcs { get; set; }

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
