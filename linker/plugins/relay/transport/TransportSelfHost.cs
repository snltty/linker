using linker.config;
using linker.plugins.relay.messenger;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using MemoryPack;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.plugins.messenger;
using linker.plugins.client;
using System.Diagnostics;

namespace linker.plugins.relay.transport
{
    public sealed class TransportSelfHost : ITransport
    {
        public string Name => "默认";
        public RelayType Type => RelayType.Linker;
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        private readonly MessengerResolver messengerResolver;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        private X509Certificate2 certificate;


        public TransportSelfHost(MessengerResolver messengerResolver, MessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config)
        {
            this.messengerResolver = messengerResolver;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;

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
                //连接中继服务器
                Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                IConnection connection = await messengerResolver.BeginReceiveClient(socket);
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayAsk,
                    Payload = MemoryPackSerializer.Serialize(relayInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);
                if (resp.Code != MessageResponeCodes.OK)
                {
                    connection.Disponse(7);
                    return null;
                }
                relayInfo.FlowingId = resp.Data.Span.ToUInt64();
                if (relayInfo.FlowingId == 0)
                {
                    connection.Disponse(7);
                    return null;
                }
                connection.Cancel();
                ClearSocket(socket);

                //通知对方，确认中继
                resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayForward,
                    Payload = MemoryPackSerializer.Serialize(relayInfo),
                });
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    connection.Disponse(7);
                    return null;
                }

                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    sslStream = new SslStream(connection.SourceNetworkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13
                    }).ConfigureAwait(false);
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
                    SSL = relayInfo.SSL,
                    BufferSize = 3
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
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<bool> OnBeginAsync(RelayInfo relayInfo, Action<ITunnelConnection> callback)
        {
            try
            {
                Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                IConnection connection = await messengerResolver.BeginReceiveClient(socket);
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayConfirm,
                    Payload = MemoryPackSerializer.Serialize(relayInfo)
                }).ConfigureAwait(false);

                connection.Cancel();
                ClearSocket(socket);

                _ = WaitSSL(connection, socket, relayInfo).ContinueWith((result) =>
                {
                    callback(result.Result);
                });

                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return false;
        }

        private async Task<TunnelConnectionTcp> WaitSSL(IConnection connection, Socket socket, RelayInfo relayInfo)
        {
            SslStream sslStream = null;
            if (relayInfo.SSL)
            {
                if (certificate == null)
                {
                    connection.Disponse(8);
                    return null;
                }
                sslStream = new SslStream(connection.SourceNetworkStream, false);
                await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
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
                SSL = relayInfo.SSL,
                BufferSize = 3,
            };
        }
        private void ClearSocket(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);
            try
            {
                while (socket.Available > 0)
                {
                    socket.Receive(buffer, SocketFlags.None);
                }
            }
            catch (Exception)
            {
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        public async Task<RelayTestResultInfo> RelayTestAsync(RelayTestInfo relayTestInfo)
        {
            IConnection connection = null;
            RelayTestResultInfo result = new RelayTestResultInfo { Delay = -1 };
            try
            {
                Socket socket = new Socket(relayTestInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayTestInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                connection = await messengerResolver.BeginReceiveClient(socket);

                var sw = new Stopwatch();
                sw.Start();
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayTest,
                    Payload = MemoryPackSerializer.Serialize(relayTestInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);
                sw.Stop();

                result.Delay = resp.Code == MessageResponeCodes.OK ? (int)sw.ElapsedMilliseconds : -1;
                result.Available = resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
                return result;
            }
            catch (Exception)
            {

            }
            finally
            {
                connection?.Disponse();
            }
            return result;
        }
    }
}
