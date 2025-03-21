
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
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 2;

        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.ttl");
        private byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.end");

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

            IPEndPoint ep = tunnelTransportInfo.Remote.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6)
                && tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6)
                ? new IPEndPoint(tunnelTransportInfo.Remote.LocalIps.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetworkV6), tunnelTransportInfo.Remote.Remote.Port)
                : tunnelTransportInfo.Remote.Remote;
            Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            try
            {
                targetSocket.KeepAlive();
                targetSocket.IPv6Only(ep.AddressFamily, false);
                targetSocket.ReuseBind(new IPEndPoint(ep.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port));

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                }
                await targetSocket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(2000)).ConfigureAwait(false);

                if (mode == TunnelMode.Client)
                {
                    return await TcpClient(tunnelTransportInfo, targetSocket).ConfigureAwait(false);
                }
                return await TcpServer(tunnelTransportInfo, targetSocket).ConfigureAwait(false);
            }
            catch (Exception)
            {
                targetSocket.SafeClose();
            }
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private async Task<ITunnelConnection> TcpClient(TunnelTransportInfo state, Socket socket)
        {
            try
            {
                //随便发个消息看对方有没有收到
                await socket.SendAsync(authBytes).ConfigureAwait(false);
                //如果对方收到，会回个消息，不管是啥，回了就行
                int length = await socket.ReceiveAsync(new byte[1024]).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                if (length == 0) return null;

                //需要ssl
                SslStream sslStream = null;
                if (state.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        ClientCertificates = new X509CertificateCollection { certificate }
                    }).ConfigureAwait(false);
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
                    Type = TunnelType.P2P,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                    SSL = state.SSL,
                    BufferSize = state.BufferSize,
                };
            }
            catch (Exception)
            { }
            return null;
        }
        private async Task<ITunnelConnection> TcpServer(TunnelTransportInfo state, Socket socket)
        {
            try
            {
                //对方会随便发个消息，不管是啥
                int length = await socket.ReceiveAsync(new byte[1024]).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
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

                    sslStream = new SslStream(new NetworkStream(socket, false), false);
                    await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false).ConfigureAwait(false);
                }

                return new TunnelConnectionTcp
                {
                    RemoteMachineId = state.Remote.MachineId,
                    RemoteMachineName = state.Remote.MachineName,
                    Direction = state.Direction,
                    ProtocolType = TunnelProtocolType.Tcp,
                    Stream = sslStream,
                    Socket = socket,
                    Type = TunnelType.P2P,
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
