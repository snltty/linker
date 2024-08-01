using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Net.Sockets;
using System.Net;
using System.Text;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.libs;
using linker.tunnel.adapter;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace linker.tunnel.transport
{
    /// <summary>
    /// 基于端口映射
    /// </summary>
    public sealed class TransportTcpPortMap : ITunnelTransport
    {
        public string Name => "TcpPortMap";

        public string Label => "Tcp端口映射";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Tcp | TunnelWanPortProtocolType.Udp;

        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly ConcurrentDictionary<string, TaskCompletionSource<Socket>> distDic = new ConcurrentDictionary<string, TaskCompletionSource<Socket>>();
        private readonly ITunnelAdapter tunnelAdapter;
        public TransportTcpPortMap(ITunnelAdapter tunnelAdapter)
        {
            this.tunnelAdapter = tunnelAdapter;
        }

        Socket socket;
        public async Task Listen(int localPort)
        {
            if (socket != null && (socket.LocalEndPoint as IPEndPoint).Port == localPort)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"{Name} {socket.LocalEndPoint} already exists");
                }
                return;
            }

            try
            {
                socket?.SafeClose();
                if (localPort == 0) return;

                IPAddress localIP = IPAddress.Any;

                socket = new Socket(localIP.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.IPv6Only(localIP.AddressFamily, false);
                socket.ReuseBind(new IPEndPoint(localIP, localPort));
                socket.Listen(int.MaxValue);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"{Name} listen {localPort}");
                }

                while (true)
                {
                    try
                    {
                        Socket client = await socket.AcceptAsync();

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                byte[] bytes = new byte[1024];
                                int length = await client.ReceiveAsync(bytes.AsMemory()).AsTask().WaitAsync(TimeSpan.FromMilliseconds(3000));
                                if (length > 0)
                                {
                                    string key = bytes.AsMemory(0, length).GetString();
                                    if (distDic.TryRemove(key, out TaskCompletionSource<Socket> tcs))
                                    {
                                        await client.SendAsync(bytes.AsMemory(0, length));
                                        tcs.SetResult(client);
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
                if (await OnSendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(100).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
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
                if (await OnSendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await task.ConfigureAwait(false);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }


            await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && tunnelAdapter.Certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                return;
            }
            //正向连接，等他来连
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                if (tunnelTransportInfo.Local.PortMapWan == 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"OnBegin WaitConnect 【{Name}】{tunnelTransportInfo.Local.MachineName} port mapping not configured");
                    await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
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
                    await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }

                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection);
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                }
                else
                {
                    await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
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
            TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>();
            string key = $"{tunnelTransportInfo.Remote.MachineId}-{tunnelTransportInfo.FlowId}";
            distDic.TryAdd(key, tcs);
            try
            {
                Socket socket = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000));

                socket.KeepAlive();
                SslStream sslStream = null;
                if (tunnelTransportInfo.SSL)
                {
                    if (tunnelAdapter.Certificate == null)
                    {
                        LoggerHelper.Instance.Error($"{Name}-> ssl Certificate not found");
                        socket.SafeClose();
                        return null;
                    }

                    sslStream = new SslStream(new NetworkStream(socket, false), false);
                    await sslStream.AuthenticateAsServerAsync(tunnelAdapter.Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
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
                    TransportName = tunnelTransportInfo.TransportName,
                    IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
                    Label = string.Empty,
                    SSL = tunnelTransportInfo.SSL,
                    BufferSize = tunnelTransportInfo.BufferSize,
                };
                return result;
            }
            catch (Exception)
            {
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

            IPEndPoint ep = new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address, tunnelTransportInfo.Remote.PortMapWan);
            Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            try
            {
                targetSocket.KeepAlive();
                targetSocket.ReuseBind(new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port));

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                }
                await targetSocket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                await targetSocket.SendAsync($"{tunnelTransportInfo.Local.MachineId}-{tunnelTransportInfo.FlowId}".ToBytes());
                await targetSocket.ReceiveAsync(new byte[1024]).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false); ;

                //需要ssl
                SslStream sslStream = null;
                if (tunnelTransportInfo.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(targetSocket, false), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 }).ConfigureAwait(false);
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
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
