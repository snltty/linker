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
    public sealed class TransportUdpQuic : ITunnelTransport
    {
        public string Name => "quic";

        public string Label => "基于UDP的Quic";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Quic;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

        private byte[] UdpTtlBytes = Encoding.UTF8.GetBytes("snltty.ttl");
        private byte[] UdpEndBytes = Encoding.UTF8.GetBytes("snltty.end");


        private X509Certificate serverCertificate;
        private IPEndPoint quicEP = new IPEndPoint(IPAddress.Any, 0);
        private ConcurrentDictionary<int, AsyncUserToken> udpListeners = new ConcurrentDictionary<int, AsyncUserToken>();

        private readonly Config config;
        public TransportUdpQuic(Config config)
        {
            this.config = config;
            _ = QuicStart();
        }

        private async Task QuicStart()
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
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
                            ServerAuthenticationOptions = new SslServerAuthenticationOptions
                            {
                                ServerCertificate = serverCertificate,
                                EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13,
                                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
                            }
                        });
                    }
                });
                quicEP = new IPEndPoint(IPAddress.Loopback, listener.LocalEndPoint.Port);

                while (true)
                {
                    QuicConnection quicConnection = await listener.AcceptConnectionAsync();
                    QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync();
                    if (udpListeners.TryGetValue(quicConnection.RemoteEndPoint.Port, out AsyncUserToken token))
                    {
                        await OnUdpConnected(token.State, quicStream, token.TargetEP);
                    }
                }
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
                BindListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1, quicEP);
                BindAndTTL(tunnelTransportInfo1);
                if (await OnSendConnectBegin(tunnelTransportInfo1) == false)
                {
                    RemoveBind(tunnelTransportInfo1.Local.Local.Port, true);
                    return null;
                }
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo);
                    return connection;
                }
                RemoveBind(tunnelTransportInfo1.Local.Local.Port, true);
            }

            await OnSendConnectFail(tunnelTransportInfo);
            return null;
        }
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                BindListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo, quicEP);
            }
            Task.Run(async () =>
            {
                if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
                {
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



            foreach (IPEndPoint ep in eps.Where(c => NotIPv6Support(c.Address) == false))
            {

                IPEndPoint local = new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port);
                UdpClient udpClient = new UdpClient(local.AddressFamily);
                udpClient.Client.ReuseBind(local);
                udpClient.Client.WindowsUdpBug();
                try
                {
                    udpClient.Send(UdpTtlBytes, ep);
                    UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(ep.Address.Equals(tunnelTransportInfo.Remote.Remote.Address) ? 500 : 100));
                    udpClient.Send(UdpEndBytes, ep);

                    int port = BindForward(udpClient, ep);

                    QuicConnection connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                    {
                        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port),
                        LocalEndPoint = new IPEndPoint(IPAddress.Any, 0),
                        DefaultCloseErrorCode = 0x0a,
                        DefaultStreamErrorCode = 0x0b,
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

                    return new TunnelConnectionQuic
                    {
                        Stream = quicStream,
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
                catch (Exception)
                {
                    udpClient.Close();
                    udpClient.Dispose();
                }
            }
            return null;
        }
        private int BindForward(UdpClient serverUdpClient, IPEndPoint server)
        {
            IPAddress localIP = NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;
            UdpClient udpClient = new UdpClient(localIP.AddressFamily);
            udpClient.Client.WindowsUdpBug();

            AsyncUserTokenForward token = new AsyncUserTokenForward { SourceUdpClient = udpClient, TargetUdpClient = serverUdpClient, TargetEP = server };
            IAsyncResult result = udpClient.BeginReceive(ReceiveCallbackUdpForward, token);

            return (udpClient.Client.LocalEndPoint as IPEndPoint).Port;
        }
        private void ReceiveCallbackUdpForward(IAsyncResult result)
        {
            AsyncUserTokenForward token = result.AsyncState as AsyncUserTokenForward;
            try
            {
                byte[] bytes = token.SourceUdpClient.EndReceive(result, ref token.tempEP);
                token.TargetUdpClient.Send(bytes, token.TargetEP);

                if (token.Received == false)
                {
                    token.Received = true;
                    AsyncUserTokenForward token1 = new AsyncUserTokenForward { SourceUdpClient = token.TargetUdpClient, TargetUdpClient = token.SourceUdpClient, TargetEP = token.tempEP, Received = true };
                    result = token.TargetUdpClient.BeginReceive(ReceiveCallbackUdpForward, token1);
                }

                result = token.SourceUdpClient.BeginReceive(ReceiveCallbackUdpForward, token);
            }
            catch (Exception)
            {
                token.Clear();
            }
        }


        private void RemoveBind(int port, bool close)
        {
            if (udpListeners.TryRemove(port, out AsyncUserToken token))
            {
                if (close)
                {
                    token.Clear();
                }
            }

        }
        private void BindListen(IPEndPoint local, TunnelTransportInfo state, IPEndPoint targetEP)
        {
            IPAddress localIP = NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any;
            UdpClient udpClient = new UdpClient(localIP.AddressFamily);
            udpClient.Client.ReuseBind(new IPEndPoint(localIP, local.Port));
            udpClient.Client.WindowsUdpBug();

            AsyncUserToken token = new AsyncUserToken { SourceUdpClient = udpClient, State = state, TargetEP = targetEP, Port = local.Port };
            IAsyncResult result = udpClient.BeginReceive(ReceiveCallbackUdp, token);

            udpListeners.AddOrUpdate(local.Port, token, (a, b) => token);
        }
        private void ReceiveCallbackUdp(IAsyncResult result)
        {
            AsyncUserToken token = result.AsyncState as AsyncUserToken;
            try
            {
                byte[] bytes = token.SourceUdpClient.EndReceive(result, ref token.tempEP);
                if (token.Received == false)
                {
                    if (bytes.AsSpan().SequenceEqual(UdpEndBytes))
                    {
                        token.Received = true;
                    }
                    else
                    {
                        token.SourceUdpClient.Send(bytes, token.tempEP);
                    }
                }
                else
                {
                    if (token.TargetUdpClient == null)
                    {
                        token.TargetUdpClient = new UdpClient();
                        token.TargetUdpClient.Client.WindowsUdpBug();
                        token.TargetUdpClient.Send(bytes, token.TargetEP);
                        AsyncUserToken token1 = new AsyncUserToken { SourceUdpClient = token.TargetUdpClient, TargetUdpClient = token.SourceUdpClient, Received = true, TargetEP = token.tempEP, Port = token.Port };
                        result = token1.SourceUdpClient.BeginReceive(ReceiveCallbackUdp, token1);
                    }
                    else
                    {
                        token.TargetUdpClient.Send(bytes, token.TargetEP);
                    }
                }

                result = token.SourceUdpClient.BeginReceive(ReceiveCallbackUdp, token);
            }
            catch (Exception)
            {
                RemoveBind(token.Port, true);
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
            foreach (var ip in eps.Where(c => NotIPv6Support(c.Address) == false))
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port);
                    using UdpClient udpClient = new UdpClient(ep.AddressFamily);
                    udpClient.Client.ReuseBind(ep);
                    udpClient.Send(new byte[] { 0 });
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
            RemoveBind(tunnelTransportInfo.Local.Local.Port, true);
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            RemoveBind(tunnelTransportInfo.Local.Local.Port, false);
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineName, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }


        private async Task OnUdpConnected(TunnelTransportInfo state, QuicStream stream, IPEndPoint remoteEndpoint)
        {
            if (state.TransportName == Name)
            {
                try
                {
                    TunnelConnectionQuic result = new TunnelConnectionQuic
                    {
                        RemoteMachineName = state.Remote.MachineName,
                        Direction = state.Direction,
                        ProtocolType = TunnelProtocolType.Quic,
                        Stream = stream,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Server,
                        TransactionId = state.TransactionId,
                        TransportName = state.TransportName,
                        IPEndPoint = remoteEndpoint,
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

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false);
        }

        public sealed class AsyncUserToken : AsyncUserTokenForward
        {
            public TunnelTransportInfo State { get; set; }
            public int Port { get; set; }
        }

        public class AsyncUserTokenForward
        {
            public UdpClient SourceUdpClient { get; set; }
            public UdpClient TargetUdpClient { get; set; }
            public IPEndPoint TargetEP { get; set; }
            public IPEndPoint tempEP = new IPEndPoint(IPAddress.Any, 0);

            public bool Received { get; set; }

            public void Clear()
            {
                try
                {
                    SourceUdpClient?.Close();
                    TargetUdpClient?.Close();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
