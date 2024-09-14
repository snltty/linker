using linker.config;
using linker.plugins.relay.messenger;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using MemoryPack;
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
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayAsk,
                    Payload = MemoryPackSerializer.Serialize(relayInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);
                if (resp.Code != MessageResponeCodes.OK)
                {
                    return null;
                }
                relayInfo.FlowingId = resp.Data.Span.ToUInt64();
                if (relayInfo.FlowingId == 0)
                {
                    return null;
                }


                //连接中继服务器
                Socket socket = new Socket(relayInfo.Server.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(relayInfo.Server).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                RelayMessage relayMessage = new RelayMessage { FlowId = relayInfo.FlowingId, Type = RelayMessengerType.Ask, FromId = relayInfo.FromMachineId, ToId = relayInfo.RemoteMachineId };
                await socket.SendAsync(relayMessage.ToBytes());

                //通知对方，确认中继
                resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayForward,
                    Payload = MemoryPackSerializer.Serialize(relayInfo),
                });
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    socket.SafeClose();
                    return null;
                }

                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
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

                RelayMessage relayMessage = new RelayMessage { FlowId = relayInfo.FlowingId, Type = RelayMessengerType.Answer, FromId = relayInfo.FromMachineId, ToId = relayInfo.RemoteMachineId };
                await socket.SendAsync(relayMessage.ToBytes());

                _ = WaitSSL(socket, relayInfo).ContinueWith((result) =>
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
                callback(null);
            }
            return false;
        }

        private async Task<TunnelConnectionTcp> WaitSSL(Socket socket, RelayInfo relayInfo)
        {
            try
            {
                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    if (certificate == null)
                    {
                        socket.SafeClose();
                        return null;
                    }
                    sslStream = new SslStream(new NetworkStream(socket, false), false);
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
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                socket.SafeClose();
            }
            return null;
        }

        public async Task<RelayTestResultInfo> RelayTestAsync(RelayTestInfo relayTestInfo)
        {
            RelayTestResultInfo result = new RelayTestResultInfo { Delay = -1 };
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
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
            return result;
        }
    }
}
