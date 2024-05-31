using cmonitor.client.tunnel;
using cmonitor.config;
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

        public string Label => "UDP,MsQuic";

        public bool Disabled => false;

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Quic;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

       

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
                BindAndTTL(tunnelTransportInfo1);
                _ = QuicStart(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
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
                    BindAndTTL(tunnelTransportInfo);
                    await Task.Delay(50);
                    _ = QuicStart(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
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


        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //先尝试内网ipv4
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //在尝试外网
           
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



            foreach (IPEndPoint ep in eps.Where(c => NetworkHelper.NotIPv6Support(c.Address) == false))
            {
                QuicConnection connection = null;
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                    {
                        RemoteEndPoint = ep,
                        LocalEndPoint = new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port),
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
                    }).AsTask().WaitAsync(TimeSpan.FromMilliseconds(ep.Address.Equals(tunnelTransportInfo.Remote.Remote.Address) ? 500 : 100));
                    

                    QuicStream quicStream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);

                    return new TunnelConnectionMsQuic
                    {
                        Stream = quicStream,
                        Connection = connection,
                        IPEndPoint = ep,
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
                        Logger.Instance.Error(ex.Message);
                    }
                    Logger.Instance.Warning($"{Name} wait 1000");
                    await Task.Delay(1000);
                }
            }
            return null;
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
            foreach (var ip in eps.Where(c => NetworkHelper.NotIPv6Support(c.Address) == false))
            {
                IPEndPoint ep = new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port);
                Socket socket = new Socket(ep.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                
                try
                {

                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineName} {ip}");
                    }
                    socket.Bind(ep);
                    socket.SendTo(Encoding.UTF8.GetBytes(tunnelTransportInfo.Remote.MachineName),ip);
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
                        socket?.SafeClose();
                    }
                    catch (Exception)
                    {
                    }
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


        private async Task OnUdpConnected(TunnelTransportInfo state, QuicConnection quicConnection, QuicStream stream)
        {
            if (state.TransportName == Name)
            {
                try
                {
                    TunnelConnectionMsQuic result = new TunnelConnectionMsQuic
                    {
                        RemoteMachineName = state.Remote.MachineName,
                        Direction = state.Direction,
                        ProtocolType = TunnelProtocolType.Quic,
                        Stream = stream,
                        Connection = quicConnection,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Server,
                        TransactionId = state.TransactionId,
                        TransportName = state.TransportName,
                        IPEndPoint = quicConnection.RemoteEndPoint,
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

        private async Task QuicStart(IPEndPoint local, TunnelTransportInfo info)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (QuicListener.IsSupported == false) return;

                QuicListener listener = await QuicListener.ListenAsync(new QuicListenerOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                    ListenBacklog = int.MaxValue,
                    ListenEndPoint = local,
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

                try
                {
                    QuicConnection quicConnection = await listener.AcceptConnectionAsync().AsTask().WaitAsync(TimeSpan.FromMilliseconds(30000));
                    QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync().AsTask().WaitAsync(TimeSpan.FromMilliseconds(2000));
                    await OnUdpConnected(info, quicConnection, quicStream);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
                await listener.DisposeAsync();
            }
        }
    }
}
