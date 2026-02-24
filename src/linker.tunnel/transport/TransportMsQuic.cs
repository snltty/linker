
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using linker.tunnel.wanport;
using System.Security.Cryptography.X509Certificates;
using linker.libs.timer;
using System.Buffers;

namespace linker.tunnel.transport
{
    /// <summary>
    /// 与UDP打洞同理，只是打洞成功后多包装一个msquic，用于保证消息准确到达
    /// </summary>
    public sealed class TransportMsQuic : ITunnelTransport
    {
        public string Name => "MsQuic";

        public string Label => "MsQuic，win10+、linux";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Quic;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public TunnelType TunnelType =>  TunnelType.P2P;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => true;

        public byte Order => 255;

        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

       

        private ConcurrentDictionary<int, ListenAsyncToken> stateDic = new ConcurrentDictionary<int, ListenAsyncToken>();
        private byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ttl1");
        private byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.end1");
        private IPEndPoint quicListenEP;

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportMsQuic(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        private X509Certificate certificate;
        public void SetSSL(X509Certificate certificate)
        {
            this.certificate = certificate;
            if (quicListenEP == null)
                _ = QuicListen();
        }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {

                if (QuicListener.IsSupported == false)
                {
                    LoggerHelper.Instance.Warning($"msquic not supported, need win11+,or linux");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return null;
                }

                if (certificate == null)
                {
                    LoggerHelper.Instance.Warning($"msquic need ssl");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return null;
                }
            }

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
                _ = ListenRemoteConnect(tunnelTransportInfo.BufferSize, tunnelTransportInfo1.Local.Local, quicListenEP, tunnelTransportInfo1);
                BindAndTTL(tunnelTransportInfo1);
                await Task.Delay(1000).ConfigureAwait(false);
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
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
        /// 收到连接请求
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
                if (QuicListener.IsSupported == false)
#pragma warning restore CA2252 // 此 API 需要选择加入预览功能
                {
                    LoggerHelper.Instance.Warning($"msquic not supported, need win11+,or linux");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }

                if (certificate == null)
                {
                    LoggerHelper.Instance.Warning($"msquic need ssl");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }
            }
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                _ = ListenRemoteConnect(tunnelTransportInfo.BufferSize, tunnelTransportInfo.Local.Local, quicListenEP, tunnelTransportInfo);
                await Task.Delay(50).ConfigureAwait(false);
                BindAndTTL(tunnelTransportInfo);
            }
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
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                        await remoteUdp.SendToAsync(authBytes, ep).ConfigureAwait(false);
                    }
                    await Task.Delay(50).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }

            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                IPEndPoint remoteEP = await taskCompletionSource.WithTimeout(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                //绑定一个udp，用来给QUIC链接
                Socket quicUdp = ListenQuicConnect(tunnelTransportInfo.BufferSize, remoteUdp, remoteEP);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
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
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                        {
                            return true;
                        }
                    }
                }, cts.Token).ConfigureAwait(false);
                QuicStream quicStream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional).ConfigureAwait(false);
#pragma warning restore CA2252 // 此 API 需要选择加入预览功能
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                return new TunnelConnectionMsQuic
                {
                    QuicUdp = quicUdp,
                    RemoteUdp = remoteUdp,
                    Stream = quicStream,
                    Connection = connection,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(remoteEP),
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
                    BufferSize = tunnelTransportInfo.BufferSize
                };
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetResult(null);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            try
            {
                remoteUdp?.SafeClose();
                remoteUdp?.SafeClose();
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
                        _ = socket.SendToAsync(Array.Empty<byte>(), SocketFlags.None, ip);
                        socket.SafeClose();
                    }
                    else
                    {
                        Socket socket = new Socket(ip.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();
                        socket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                        socket.Ttl = 2;
                        _ = socket.SendToAsync(endBytes, SocketFlags.None, ip);
                        socket.SafeClose();
                    }
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
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.WithTimeout(TimeSpan.FromMilliseconds(10000)).ConfigureAwait(false);
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

            TimerHelper.Async(async () =>
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent((1 << bufferSize) * 1024);
                try
                {
                    IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

                    //收到远端的消息，表明对方已收到，再给它发个结束消息，表示可以正常通信了
                    SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer.AsMemory(), tempEp).ConfigureAwait(false);
                    IPEndPoint ep = result.RemoteEndPoint as IPEndPoint;

                    await socketUdp.SendToAsync(endBytes, ep).ConfigureAwait(false);
                    tcs.TrySetResult(ep);
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

                await Task.WhenAny(CopyToAsync(bufferSize, localUdp, remoteUdp, remoteEP), CopyToAsync(bufferSize, remoteUdp, localUdp, quicEp)).ConfigureAwait(false);
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

            TaskCompletionSource<AddressFamily> tcs = new TaskCompletionSource<AddressFamily>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                udpClient.ReuseBind(local);
                udpClient.WindowsUdpBug();
                _ = WaitAuth(bufferSize, token, tcs);

                AddressFamily af = await tcs.WithTimeout(TimeSpan.FromMilliseconds(30000)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(AddressFamily.InterNetwork);
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
                    if (result.ReceivedBytes == 0) continue;

                    Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);

                    //是认证结束的消息，表示双方能通信了，接下来直接跟QUIC交换数据就可以了
                    if (memory.Length == endBytes.Length && memory.Span.SequenceEqual(endBytes))
                    {
                        if (tcs != null && tcs.Task.IsCompleted == false)
                        {
                            token.RemoteEP = result.RemoteEndPoint as IPEndPoint;
                            tcs.TrySetResult(result.RemoteEndPoint.AddressFamily);
                            _ = Connect2Quic(bufferSize, token);
                        }

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
                await Task.WhenAny(CopyToAsync(bufferSize, token.RemoteUdp, token.QuicUdp, token.QuicEP), CopyToAsync(bufferSize, token.QuicUdp, token.RemoteUdp, token.RemoteEP)).ConfigureAwait(false);
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
                    if (result.ReceivedBytes == 0)
                    {
                        continue;
                    }
                    if (result.ReceivedBytes == endBytes.Length && buffer.AsMemory(0, result.ReceivedBytes).Span.SequenceEqual(endBytes))
                    {

                    }
                    else
                    {
                        await remote.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), remoteEp).ConfigureAwait(false);
                    }
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
                tcs.TrySetResult(null);
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
                tcs.TrySetResult(null);
            }
        }

#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        private async Task OnUdpConnected(object _state, Socket remoteUdp, Socket quicUdp, IPEndPoint remoteEP, QuicConnection quicConnection, QuicStream stream)
#pragma warning restore CA2252 // 此 API 需要选择加入预览功能
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
                        Type = TunnelType,
                        Mode = TunnelMode.Server,
                        TransactionId = state.TransactionId,
                        TransactionTag = state.TransactionTag,
                        TransportName = state.TransportName,
                        IPEndPoint = NetworkHelper.TransEndpointFamily(remoteEP),
                        Label = string.Empty,
                        BufferSize = state.BufferSize,
                    };
                    if (reverseDic.TryRemove(state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
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
                }
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// QUIC监听
        /// </summary>
        /// <returns></returns>
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        private async Task QuicListen()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {

                if (QuicListener.IsSupported == false)
                {
                    LoggerHelper.Instance.Warning($"msquic not supported, need win11+,or linux, or try to restart linker");
                    return;
                }
                if (certificate == null)
                {
                    LoggerHelper.Instance.Warning($"msquic need ssl");
                    return;
                }
                QuicListener listener = await QuicListener.ListenAsync(new QuicListenerOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                    ListenBacklog = int.MaxValue,
                    ListenEndPoint = new IPEndPoint(IPAddress.Any, 0),
                    ConnectionOptionsCallback = (connection, hello, token) =>
                    {
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                        return ValueTask.FromResult(new QuicServerConnectionOptions
                        {
                            MaxInboundBidirectionalStreams = 65535,
                            MaxInboundUnidirectionalStreams = 65535,
                            DefaultCloseErrorCode = 0x0a,
                            DefaultStreamErrorCode = 0x0b,
                            IdleTimeout = TimeSpan.FromMilliseconds(15000),
                            ServerAuthenticationOptions = new SslServerAuthenticationOptions
                            {
                                ServerCertificate = certificate,
                                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
                            }
                        });
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                    }
                }).ConfigureAwait(false);

                quicListenEP = new IPEndPoint(IPAddress.Loopback, listener.LocalEndPoint.Port);
                while (true)
                {
                    try
                    {
                        QuicConnection quicConnection = await listener.AcceptConnectionAsync().ConfigureAwait(false);
                        TimerHelper.Async(async () =>
                        {
                            while (true)
                            {
                                QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);

                                if (stateDic.TryRemove(quicConnection.RemoteEndPoint.Port, out ListenAsyncToken token))
                                {
                                    await OnUdpConnected(token.State, token.RemoteUdp, token.QuicUdp, token.RemoteEP, quicConnection, quicStream).ConfigureAwait(false);
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
                        break;
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

    }
}
