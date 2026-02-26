
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using linker.tunnel.wanport;
using System.Buffers;

namespace linker.tunnel.transport
{
    /// <summary>
    /// TCP同时连接打洞
    /// 
    /// 大致原理
    /// A 通知 B ，B立马连接A，A也同时去连接B
    /// </summary>
    public sealed class TransportTcpP2PNAT : ITunnelTransport
    {
        public string Name => "TcpP2PNAT";
        public string Label => "TCP、同时打开";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Tcp;
        public TunnelType TunnelType => TunnelType.P2P;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 4;

        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.ttl1");
        private readonly byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.tcp.end1");

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportTcpP2PNAT(ITunnelMessengerAdapter tunnelMessengerAdapter)
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
            if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
            {
                return null;
            }
            if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Client).ConfigureAwait(false);
            if (connection != null)
            {
                await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                return connection;
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
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Server).ConfigureAwait(false);
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

        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {

        }

        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo, TunnelMode mode)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            for (int i = 0; i < 5; i++)
            {
                var results = (await Task.WhenAll(tunnelTransportInfo.RemoteEndPoints.Select(ConnectAsync).ToList())).Where(c => c.Item1).ToList();
                if (results.Count == 0) continue;
                for (int j = 1; j < results.Count; j++) results[j].Item2.SafeClose();

                return mode == TunnelMode.Client
                    ? await TcpClient(tunnelTransportInfo, results[0].Item2).ConfigureAwait(false)
                    : await TcpServer(tunnelTransportInfo, results[0].Item2).ConfigureAwait(false);

            }
            return null;

            async Task<ValueTuple<bool, Socket>> ConnectAsync(IPEndPoint ep)
            {
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                try
                {
                    targetSocket.NoDelay = true;
                    targetSocket.KeepAlive();
                    targetSocket.IPv6Only(ep.AddressFamily, false);
                    targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    await targetSocket.ConnectAsync(ep,cts.Token).ConfigureAwait(false);

                    return (true, targetSocket);
                }
                catch (Exception)
                {
                    targetSocket.SafeClose();
                }
                return (false, null);
            }
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private async Task<ITunnelConnection> TcpClient(TunnelTransportInfo state, Socket socket)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(4 * 1024);
            using CancellationTokenSource cts = new CancellationTokenSource(500);
            try
            {
                //随便发个消息看对方有没有收到
                await socket.SendAsync(authBytes).ConfigureAwait(false);
                //如果对方收到，会回个消息，不管是啥，回了就行
                int length = await socket.ReceiveAsync(buffer.Memory,cts.Token).ConfigureAwait(false);
                if (length == 0) return null;

                //需要ssl
                SslStream sslStream = null;
                if (state.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false, ValidateServerCertificate, null);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        ClientCertificates = new X509CertificateCollection { certificate }
                    }).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                }

                return new TunnelConnectionTcp
                {
                    Stream = sslStream,
                    Socket = socket,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
                    TransactionId = state.TransactionId,
                    TransactionTag = state.TransactionTag,
                    RemoteMachineId = state.Remote.MachineId,
                    RemoteMachineName = state.Remote.MachineName,
                    TransportName = Name,
                    Direction = state.Direction,
                    ProtocolType = ProtocolType,
                    Type = TunnelType,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                    SSL = state.SSL,
                    BufferSize = state.BufferSize,
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return null;
        }
        private async Task<ITunnelConnection> TcpServer(TunnelTransportInfo state, Socket socket)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(4 * 1024);
            using CancellationTokenSource cts = new CancellationTokenSource(500);
            try
            {
                //对方会随便发个消息，不管是啥
                int length = await socket.ReceiveAsync(buffer.Memory,cts.Token).ConfigureAwait(false);
                if (length == 0)
                {
                    return null;
                }
                //回个消息给它就完了
                await socket.SendAsync(endBytes).ConfigureAwait(false);

                socket.KeepAlive();
                SslStream sslStream = null;
                if (state.SSL)
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

                return new TunnelConnectionTcp
                {
                    RemoteMachineId = state.Remote.MachineId,
                    RemoteMachineName = state.Remote.MachineName,
                    Direction = state.Direction,
                    ProtocolType = TunnelProtocolType.Tcp,
                    Stream = sslStream,
                    Socket = socket,
                    Type = TunnelType,
                    Mode = TunnelMode.Server,
                    TransactionId = state.TransactionId,
                    TransactionTag = state.TransactionTag,
                    TransportName = state.TransportName,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
                    Label = string.Empty,
                    SSL = state.SSL,
                    BufferSize = state.BufferSize,
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return null;
        }
    }
}
