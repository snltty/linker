using cmonitor.config;
using cmonitor.plugins.relay.messenger;
using cmonitor.server;
using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace cmonitor.plugins.relay.transport
{
    public sealed class TransportSelfHost : ITransport
    {
        public string Name => "默认";
        public RelayCompactType Type => RelayCompactType.Cmonitor;
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        private readonly TcpServer tcpServer;
        private readonly MessengerSender messengerSender;

        private X509Certificate2 certificate;


        public TransportSelfHost(TcpServer tcpServer, MessengerSender messengerSender, Config config)
        {
            this.tcpServer = tcpServer;
            this.messengerSender = messengerSender;

            string path = Path.GetFullPath(config.Data.Client.Certificate);
            if (File.Exists(path))
            {
                certificate = new X509Certificate2(path, config.Data.Client.Password, X509KeyStorageFlags.Exportable);
            }
        }

        public async Task<ITunnelConnection> RelayAsync(RelayInfo relayInfo)
        {
            try
            {
                Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500));

                IConnection connection = await tcpServer.BeginReceive(socket);
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayForward,
                    Payload = MemoryPackSerializer.Serialize(relayInfo),
                    Timeout = 2000
                });
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    connection.Disponse(7);
                    return null;
                }

                connection.Cancel();
                await Task.Delay(500);
                ClearSocket(socket);

                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    sslStream = new SslStream(connection.SourceNetworkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13
                    });
                }

                return new TunnelConnectionTcp
                {
                    Direction = TunnelDirection.Forward,
                    ProtocolType = TunnelProtocolType.Tcp,
                    RemoteMachineId = relayInfo.RemoteMachineId,
                    RemoteMachineName = relayInfo.RemoteMachineName,
                    Stream = sslStream,
                    Socket = socket,
                    Mode = TunnelMode.Client,
                    IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
                    TransactionId = relayInfo.TransactionId,
                    TransportName = Name,
                    Type = TunnelType.Relay,
                    SSL = relayInfo.SSL
                };
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            return null;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public async Task<ITunnelConnection> OnBeginAsync(RelayInfo relayInfo)
        {
            try
            {
                Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500));

                IConnection connection = await tcpServer.BeginReceive(socket);
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayForward,
                    Payload = MemoryPackSerializer.Serialize(relayInfo)
                });
                connection.Cancel();
                await Task.Delay(100);
                ClearSocket(socket);

                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    if (certificate == null)
                    {
                        connection.Disponse(8);
                        return null;
                    }
                    sslStream = new SslStream(connection.SourceNetworkStream, false);
                    await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false);
                }
                return new TunnelConnectionTcp
                {
                    Direction = TunnelDirection.Reverse,
                    ProtocolType = TunnelProtocolType.Tcp,
                    RemoteMachineId = relayInfo.RemoteMachineId,
                    RemoteMachineName = relayInfo.RemoteMachineName,
                    Stream = sslStream,
                    Socket = socket,
                    Mode = TunnelMode.Server,
                    IPEndPoint = socket.RemoteEndPoint as IPEndPoint,
                    TransactionId = relayInfo.TransactionId,
                    TransportName = Name,
                    Type = TunnelType.Relay,
                    SSL = relayInfo.SSL
                };
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            return null;
        }


        public void ClearSocket(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);
            while (socket.Available > 0)
            {
                socket.Receive(buffer, SocketFlags.None);
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
