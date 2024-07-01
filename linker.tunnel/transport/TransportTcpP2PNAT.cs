using linker.tunnel.adapter;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace linker.tunnel.transport
{
    public sealed class TransportTcpP2PNAT : ITunnelTransport
    {
        public string Name => "TcpP2PNAT";
        public string Label => "TCP、同时打开";
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        /// <summary>
        /// 发送开始连接消息
        /// </summary>
        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        /// <summary>
        /// 发送连接失败消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        /// <summary>
        /// 发送连接成功消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        /// <summary>
        /// 连接成功
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.ttl");
        private byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.end");


        private readonly ITunnelAdapter tunnelAdapter;
        public TransportTcpP2PNAT(ITunnelAdapter tunnelAdapter)
        {
            this.tunnelAdapter = tunnelAdapter;
        }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (await OnSendConnectBegin(tunnelTransportInfo) == false)
            {
                return null;
            }
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Client);
            if (connection != null)
            {
                await OnSendConnectSuccess(tunnelTransportInfo);
                return connection;
            }

            await OnSendConnectFail(tunnelTransportInfo);
            return null;
        }
        /// <summary>
        /// 收到对方开始连接的消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.SSL && tunnelAdapter.Certificate == null)
            {
                LoggerHelper.Instance.Error($"{Name}->ssl Certificate not found");
                await OnSendConnectSuccess(tunnelTransportInfo);
                return;
            }
            ITunnelConnection connection = await ConnectForward(tunnelTransportInfo, TunnelMode.Server);
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

            IPEndPoint ep = tunnelTransportInfo.Remote.Remote;
            Socket targetSocket = new(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            try
            {
                targetSocket.KeepAlive();
                targetSocket.ReuseBind(new IPEndPoint(IPAddress.Any, tunnelTransportInfo.Local.Local.Port));

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                }
                await targetSocket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(500));

                if (mode == TunnelMode.Client)
                {
                    return await TcpClient(tunnelTransportInfo, targetSocket);
                }
                return await TcpServer(tunnelTransportInfo, targetSocket);
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
            //需要ssl
            SslStream sslStream = null;
            if (state.SSL)
            {
                sslStream = new SslStream(new NetworkStream(socket, false), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 });
            }

            return new TunnelConnectionTcp
            {
                Stream = sslStream,
                Socket = socket,
                IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
                TransactionId = state.TransactionId,
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
        private async Task<ITunnelConnection> TcpServer(TunnelTransportInfo state, Socket socket)
        {
            try
            {
                socket.KeepAlive();
                SslStream sslStream = null;
                if (state.SSL)
                {
                    if (tunnelAdapter.Certificate == null)
                    {
                        LoggerHelper.Instance.Error($"{Name}-> ssl Certificate not found");
                        socket.SafeClose();
                        return null;
                    }

                    sslStream = new SslStream(new NetworkStream(socket, false), false);
                    await sslStream.AuthenticateAsServerAsync(tunnelAdapter.Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false);
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
                    TransportName = state.TransportName,
                    IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
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
