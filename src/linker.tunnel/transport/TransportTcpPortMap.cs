using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Net.Sockets;
using System.Net;
using System.Text;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.libs;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using linker.libs.timer;
using System.Buffers;

namespace linker.tunnel.transport
{
    /// <summary>
    /// 基于端口映射
    /// 这个没什么说的，就是设置了固定端口，就监听，对方来连这个固定的端口即可
    /// </summary>
    public sealed class TransportTcpPortMap : ITunnelTransport
    {
        public string Name => "TcpPortMap";

        public string Label => "TCP、端口映射";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Tcp | TunnelWanPortProtocolType.Udp;

        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 2;

        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly ConcurrentDictionary<string, TaskCompletionSource<Socket>> distDic = new ConcurrentDictionary<string, TaskCompletionSource<Socket>>();

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportTcpPortMap(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        private X509Certificate certificate;
        public void SetSSL(X509Certificate certificate)
        {
            this.certificate = certificate;
        }


        Socket socket;
        SemaphoreSlim slim = new SemaphoreSlim(1);
        public async Task Listen(int localPort)
        {
            await slim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (localPort == 0) return;

                if (socket != null && (socket.LocalEndPoint as IPEndPoint).Port == localPort)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} {socket.LocalEndPoint} already exists");
                    }
                    return;
                }

                socket?.SafeClose();

                IPAddress localIP = IPAddress.IPv6Any;

                Socket _socket = new Socket(localIP.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                _socket.IPv6Only(localIP.AddressFamily, false);
                _socket.Bind(new IPEndPoint(localIP, localPort));
                _socket.Listen(int.MaxValue);
                socket = _socket;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"{Name} listen {localPort}");
                }

                while (true)
                {
                    try
                    {
                        Socket client = await socket.AcceptAsync().ConfigureAwait(false);

                        TimerHelper.Async(async () =>
                        {
                            using CancellationTokenSource cts = new CancellationTokenSource(3000);
                            try
                            {
                                using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
                                int length = await client.ReceiveAsync(buffer.Memory, cts.Token).ConfigureAwait(false);
                                if (length > 0)
                                {
                                    string key = buffer.Memory.Slice(0, length).GetString();
                                    if (distDic.TryRemove(key, out TaskCompletionSource<Socket> tcs))
                                    {
                                        await client.SendAsync(buffer.Memory.Slice(0, length)).ConfigureAwait(false);
                                        tcs.TrySetResult(client);
                                        return;
                                    }
                                }
                                client.SafeClose();
                            }
                            catch (Exception)
                            {
                                client.SafeClose();
                            }
                        });
                    }
                    catch (Exception)
                    {
                        break;
                    }
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
                slim.Release();
            }
        }

        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                if (tunnelTransportInfo.Remote.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"ConnectAsync Forward【{Name}】{tunnelTransportInfo.Remote.MachineName} port mapping not configured");
                    return null;
                }
                //正向连接
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(100).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                if (tunnelTransportInfo.Local.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"ConnectAsync Reverse【{Name}】{tunnelTransportInfo.Local.MachineName} port mapping not configured");
                    return null;
                }
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                //等待对方连接，如果连接成功，我会收到一个socket，并且创建一个连接对象，失败的话会超时，那就是null
                var task = WaitConnect(tunnelTransportInfo1);
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await task.ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }


            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                return;
            }
            //正向连接，等他来连
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                if (tunnelTransportInfo.Local.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OnBegin WaitConnect 【{Name}】{tunnelTransportInfo.Local.MachineName} port mapping not configured");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }
                _ = WaitConnect(tunnelTransportInfo).ContinueWith((result) =>
                {
                    OnConnected(result.Result);
                });
            }
            //我要连它，那就连接
            else
            {
                if (tunnelTransportInfo.Remote.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OnBegin ConnectForward 【{Name}】{tunnelTransportInfo.Remote.MachineName} port mapping not configured");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }

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
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
        }

        private async Task<ITunnelConnection> WaitConnect(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>(TaskCreationOptions.RunContinuationsAsynchronously);
            string key = $"{tunnelTransportInfo.Remote.MachineId}-{tunnelTransportInfo.FlowId}";
            distDic.TryAdd(key, tcs);
            try
            {
                Socket socket = await tcs.WithTimeout(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                socket.KeepAlive();
                SslStream sslStream = null;
                if (tunnelTransportInfo.SSL)
                {
                    if (certificate == null)
                    {
                        LoggerHelper.Instance.Error($"{Name}-> ssl Certificate not found");
                        socket.SafeClose();
                        return null;
                    }

                    sslStream = new SslStream(new NetworkStream(socket, false), false, ValidateServerCertificate, null);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                    await sslStream.AuthenticateAsServerAsync(certificate, OperatingSystem.IsAndroid(), SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                }

                TunnelConnectionTcp result = new TunnelConnectionTcp
                {
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    Direction = tunnelTransportInfo.Direction,
                    ProtocolType = TunnelProtocolType.Tcp,
                    Stream = sslStream,
                    Socket = socket,
                    Type = TunnelType.P2P,
                    Mode = TunnelMode.Server,
                    TransactionId = tunnelTransportInfo.TransactionId,
                    TransactionTag = tunnelTransportInfo.TransactionTag,
                    TransportName = tunnelTransportInfo.TransportName,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
                    Label = string.Empty,
                    SSL = tunnelTransportInfo.SSL,
                    BufferSize = tunnelTransportInfo.BufferSize,
                };
                return result;
            }
            catch (Exception)
            {
                tcs.TrySetResult(null);
            }
            finally
            {
                distDic.TryRemove(key, out _);
            }
            return null;
        }
        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            List<IPEndPoint> eps = tunnelTransportInfo.RemoteEndPoints.Select(c => c.Address).Distinct().Select(c => new IPEndPoint(c, tunnelTransportInfo.Remote.PortMapWan)).ToList();

            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);

            foreach (var ep in eps)
            {
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                using CancellationTokenSource cts = new CancellationTokenSource(500);
                try
                {
                    targetSocket.KeepAlive();
                    targetSocket.IPv6Only(ep.AddressFamily, false);
                    targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    await targetSocket.ConnectAsync(ep, cts.Token).ConfigureAwait(false);

                    await targetSocket.SendAsync($"{tunnelTransportInfo.Local.MachineId}-{tunnelTransportInfo.FlowId}".ToBytes()).ConfigureAwait(false);
                    await targetSocket.ReceiveAsync(buffer.Memory, cts.Token).ConfigureAwait(false);

                    //需要ssl
                    SslStream sslStream = null;
                    if (tunnelTransportInfo.SSL)
                    {
                        sslStream = new SslStream(new NetworkStream(targetSocket, false), false, ValidateServerCertificate, null);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                            ClientCertificates = new X509CertificateCollection { certificate }
                        }, cts.Token).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                    }

                    return new TunnelConnectionTcp
                    {
                        Stream = sslStream,
                        Socket = targetSocket,
                        IPEndPoint = NetworkHelper.TransEndpointFamily(targetSocket.RemoteEndPoint as IPEndPoint),
                        TransactionId = tunnelTransportInfo.TransactionId,
                        TransactionTag = tunnelTransportInfo.TransactionTag,
                        RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                        RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                        TransportName = Name,
                        Direction = tunnelTransportInfo.Direction,
                        ProtocolType = ProtocolType,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Client,
                        Label = string.Empty,
                        SSL = tunnelTransportInfo.SSL,
                        BufferSize = tunnelTransportInfo.BufferSize,
                    };
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"{Name} connect {ep} fail {ex}");
                    }
                    targetSocket.SafeClose();
                }
            }
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
