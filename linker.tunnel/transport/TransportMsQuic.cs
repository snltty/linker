using linker.tunnel.adapter;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace linker.tunnel.transport
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
            _ = QuicListen();

            /*
             *  QUIC监听 QuicStart
             * 
             *  大致流程
             *  
             *  1、ConnectAsync 告诉B，我要连接你
             *  
             *  2、B 收到消息调用 OnBegin，然后绑定一个 socketB 等待 A 的消息，
             *  3、B 给 A 发送一些消息 ，在 BindAndTTL，
             *  
             *  4、A 绑定一个监听 socketA，然后发消息给 B , 如果 B 收到消息，就会回一条消息，这个监听就会收到消息，在 ConnectForward
             *  5、A 再绑定一个 socketA1，用以接收quic的连接，
             *  
             *  6、socketA1 收到消息，则通过 socketA 发送给B， socketB 收到消息，创建一个udp，发送给quic监听，完成一个线路
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
                    LoggerHelper.Instance.Error($"msquic not supported, need win11+,or linux");
                    await OnSendConnectFail(tunnelTransportInfo);
                    return null;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    LoggerHelper.Instance.Error($"msquic need ssl");
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
                await Task.Delay(1000);
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
                _ = ListenRemoteConnect(tunnelTransportInfo.BufferSize, tunnelTransportInfo1.Local.Local, quicListenEP, tunnelTransportInfo1);
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
                    LoggerHelper.Instance.Error($"msquic not supported, need win11+,or linux");
                    await OnSendConnectFail(tunnelTransportInfo);
                    return;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    LoggerHelper.Instance.Error($"msquic need ssl");
                    await OnSendConnectFail(tunnelTransportInfo);
                    return;
                }
            }
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                _ = ListenRemoteConnect(tunnelTransportInfo.BufferSize, tunnelTransportInfo.Local.Local, quicListenEP, tunnelTransportInfo);
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
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>();
            //接收远端数据，收到了就是成功了
            Socket remoteUdp = ListenRemoteCallback(tunnelTransportInfo.BufferSize, local, taskCompletionSource);

            //给远端发送一些消息
            foreach (IPEndPoint ep in tunnelTransportInfo.RemoteEndPoints)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    if (ep.AddressFamily == AddressFamily.InterNetwork)
                    {
                        await remoteUdp.SendToAsync(authBytes, ep);
                    }
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex.Message);
                    }
                }
            }

            try
            {
                IPEndPoint remoteEP = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
                //绑定一个udp，用来给QUIC链接
                Socket quicUdp = ListenQuicConnect(tunnelTransportInfo.BufferSize, remoteUdp, remoteEP);
                QuicConnection connection = connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, (quicUdp.LocalEndPoint as IPEndPoint).Port),
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
                    QuicUdp = quicUdp,
                    RemoteUdp = remoteUdp,
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
                    BufferSize = tunnelTransportInfo.BufferSize
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            try
            {
                remoteUdp?.Close();
                remoteUdp?.Close();
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
            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in tunnelTransportInfo.RemoteEndPoints)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ip}");
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
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex.Message);
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
        private Socket ListenRemoteCallback(byte bufferSize, IPEndPoint local, TaskCompletionSource<IPEndPoint> tcs)
        {
            Socket socketUdp = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            socketUdp.ReuseBind(local);
            socketUdp.WindowsUdpBug();

            Task.Run(async () =>
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
                try
                {
                    IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

                    //收到远端的消息，表明对方已收到，再给它发个结束消息，表示可以正常通信了
                    SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer.AsMemory(), tempEp);
                    IPEndPoint ep = result.RemoteEndPoint as IPEndPoint;

                    await socketUdp.SendToAsync(endBytes, ep);
                    tcs.SetResult(ep);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            });
            return socketUdp;
        }


        /// <summary>
        /// 监听UDP，等QUIC连接
        /// </summary>
        /// <param name="remoteUdp">监听收到消息消息后，通过这个udp发送给远端</param>
        /// <param name="remoteEP">远端地址</param>
        /// <returns></returns>
        private Socket ListenQuicConnect(byte bufferSize, Socket remoteUdp, IPEndPoint remoteEP)
        {
            Socket localUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            localUdp.Bind(new IPEndPoint(IPAddress.Any, 0));
            localUdp.WindowsUdpBug();

            _ = WaitQuicConnect(bufferSize, remoteUdp, remoteEP, localUdp);

            return localUdp;
        }
        /// <summary>
        /// 等待QUIC来连接
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task WaitQuicConnect(byte bufferSize, Socket remoteUdp, IPEndPoint remoteEP, Socket localUdp)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                SocketReceiveFromResult result = await localUdp.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);
                //quic的地址
                IPEndPoint quicEp = result.RemoteEndPoint as IPEndPoint;
                //发送给远端
                await remoteUdp.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), remoteEP).ConfigureAwait(false);

                await Task.WhenAll(CopyToAsync(bufferSize, localUdp, remoteUdp, remoteEP), CopyToAsync(bufferSize, remoteUdp, localUdp, quicEp)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                localUdp.SafeClose();
                remoteUdp.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 监听UDP，等待对方发来消息，然后再回消息给它，以确定能通信
        /// </summary>
        /// <param name="local">UDP监听地址</param>
        /// <param name="quicEP">QUIC监听地址</param>
        /// <param name="state">收到连接后，调用连接成功回调，带上这个信息</param>
        /// <returns></returns>
        private async Task ListenRemoteConnect(byte bufferSize, IPEndPoint local, IPEndPoint quicEP, TunnelTransportInfo state)
        {
            Socket udpClient = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            ListenAsyncToken token = new ListenAsyncToken
            {
                RemoteUdp = udpClient,
                QuicEP = quicEP,
                State = state
            };
            try
            {
                TaskCompletionSource<AddressFamily> tcs = new TaskCompletionSource<AddressFamily>();

                udpClient.ReuseBind(local);
                udpClient.WindowsUdpBug();
                _ = WaitAuth(bufferSize, token, tcs);

                AddressFamily af = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(30000));
            }
            catch (Exception ex)
            {
                token.Clear();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        /// <summary>
        /// 等待认证信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task WaitAuth(byte bufferSize, ListenAsyncToken token, TaskCompletionSource<AddressFamily> tcs)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                while (true)
                {
                    SocketReceiveFromResult result = await token.RemoteUdp.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0) return;

                    Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);

                    //是认证结束的消息，表示双方能通信了，接下来直接跟QUIC交换数据就可以了
                    if (memory.Length == endBytes.Length && memory.Span.SequenceEqual(endBytes))
                    {
                        token.RemoteEP = result.RemoteEndPoint as IPEndPoint;
                        tcs.SetResult(result.RemoteEndPoint.AddressFamily);
                        _ = Connect2Quic(bufferSize, token);
                        break;
                    }
                    else
                    {
                        //否则原样返回消息，让对方知道我收到了消息
                        await token.RemoteUdp.SendToAsync(memory, result.RemoteEndPoint).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                token.Clear();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        /// <summary>
        /// 连接到QUIC监听
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task Connect2Quic(byte bufferSize, ListenAsyncToken token)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                //等待对方来一条消息
                SocketReceiveFromResult result = await token.RemoteUdp.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);

                //发给QUIC监听，因为UDP，必须先发一条数据，然后才能接收，所以，先给QUIC发一条，才能拿去交换数据
                token.QuicUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                token.QuicUdp.WindowsUdpBug();
                await token.QuicUdp.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), token.QuicEP).ConfigureAwait(false);
                stateDic.AddOrUpdate((token.QuicUdp.LocalEndPoint as IPEndPoint).Port, token, (a, b) => token);

                //然后就可以交换数据了
                await Task.WhenAll(CopyToAsync(bufferSize, token.RemoteUdp, token.QuicUdp, token.QuicEP), CopyToAsync(bufferSize, token.QuicUdp, token.RemoteUdp, token.RemoteEP)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                token.Clear();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 交换数据
        /// </summary>
        /// <param name="local"></param>
        /// <param name="remote"></param>
        /// <param name="remoteEp"></param>
        /// <returns></returns>
        private async Task CopyToAsync(byte bufferSize, Socket local, Socket remote, IPEndPoint remoteEp)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                while (true)
                {
                    SocketReceiveFromResult result = await local.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0) break;
                    await remote.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), remoteEp).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                local.SafeClose();
                remote.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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

        private async Task OnUdpConnected(object _state, Socket remoteUdp, Socket quicUdp, IPEndPoint remoteEP, QuicConnection quicConnection, QuicStream stream)
        {
            TunnelTransportInfo state = _state as TunnelTransportInfo;
            if (state.TransportName == Name)
            {
                try
                {
                    TunnelConnectionMsQuic result = new TunnelConnectionMsQuic
                    {
                        QuicUdp = quicUdp,
                        RemoteUdp = remoteUdp,
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
                        BufferSize = state.BufferSize,
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
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// QUIC监听
        /// </summary>
        /// <returns></returns>
        private async Task QuicListen()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false)
                {
                    LoggerHelper.Instance.Error($"msquic not supported, need win11+,or linux");
                    return;
                }
                if (tunnelAdapter.Certificate == null)
                {
                    LoggerHelper.Instance.Error($"msquic need ssl");
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
                                    await OnUdpConnected(token.State, token.RemoteUdp, token.QuicUdp, token.RemoteEP, quicConnection, quicStream);
                                }
                            }
                        });
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
        }

        sealed class ListenAsyncToken
        {
            /// <summary>
            /// 和QUIC通信的
            /// </summary>
            public Socket QuicUdp { get; set; }
            public IPEndPoint QuicEP { get; set; }

            /// <summary>
            /// 和远端通信的
            /// </summary>
            public Socket RemoteUdp { get; set; }
            public IPEndPoint RemoteEP { get; set; }
            public object State { get; set; }

            public void Clear()
            {
                QuicUdp?.SafeClose();
                RemoteUdp?.SafeClose();
            }
        }

        enum ListenStep : byte
        {
            Auth = 0,
            Forward = 1
        }
    }
}
