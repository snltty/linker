using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace cmonitor.server
{
    public sealed class TcpServer
    {
        private Socket socket;
        private UdpClient socketUdp;
        private CancellationTokenSource cancellationTokenSource;
        private X509Certificate serverCertificate;

        private readonly IConnectionReceiveCallback connectionReceiveCallback;
        public TcpServer(MessengerResolver connectionReceiveCallback)
        {
            cancellationTokenSource = new CancellationTokenSource();
            this.connectionReceiveCallback = connectionReceiveCallback;
        }

        public void Init(string certificate, string password)
        {
            string path = Path.GetFullPath(certificate);
            if (File.Exists(path))
            {
                serverCertificate = new X509Certificate(path, password);
            }
            else
            {
                Logger.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
        }

        public void Start(int port)
        {
            if (socket == null)
            {
                socket = BindAccept(port);
            }
        }
        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = socket,
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            socketUdp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            //socketUdp.JoinMulticastGroup(config.BroadcastIP);
            socketUdp.Client.EnableBroadcast = true;
            socketUdp.Client.WindowsUdpBug();
            IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);

            return socket;
        }

        byte[] sendData = new byte[20];
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] bytes = socketUdp.EndReceive(result, ref endPoint);

                try
                {
                    sendData[0] = (byte)endPoint.AddressFamily;
                    endPoint.Address.TryWriteBytes(sendData.AsSpan(1), out int length);
                    ((ushort)endPoint.Port).ToBytes(sendData.AsMemory(1 + length));

                    for (int i = 0; i < 1 + length + 2; i++)
                    {
                        sendData[i] = (byte)(sendData[i] ^ byte.MaxValue);
                    }

                    await socketUdp.SendAsync(sendData.AsMemory(0, 1 + length + 2), endPoint);
                }
                catch (Exception)
                {
                }

                result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
            }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            Socket token = (Socket)acceptEventArg.UserToken;
            try
            {
                if (token.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
                token?.SafeClose();
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket != null)
            {
                _ = BeginReceiveServer(e.AcceptSocket);
                StartAccept(e);
            }
        }
        private async Task<IConnection> BeginReceiveServer(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                SslStream sslStream = new SslStream(new NetworkStream(socket), true);
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false);
                IConnection connection = CreateConnection(sslStream, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);


                connection.BeginReceive(connectionReceiveCallback,null,true);
                return connection;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            return null;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<IConnection> BeginReceive(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                NetworkStream networkStream = new NetworkStream(socket, true);
                SslStream sslStream = new SslStream(networkStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 });
                IConnection connection = CreateConnection(sslStream, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

                connection.BeginReceive(connectionReceiveCallback, null, true);

                return connection;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            return null;
        }

        public IConnection CreateConnection(SslStream stream, IPEndPoint local, IPEndPoint remote)
        {
            return new TcpConnection(stream, local, remote)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
        }
        public void Disponse()
        {
            Stop();
        }
    }
}
