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
using System.Buffers;
using linker.plugins.relay.server;

namespace linker.plugins.relay.client.transport
{
    public sealed class TransportSelfHost : ITransport
    {
        public string Name => "Linker";
        public RelayType Type => RelayType.Linker;
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        private X509Certificate2 certificate;


        public TransportSelfHost(IMessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;

            string path = Path.GetFullPath(config.Data.Client.SSL.File);
            if (File.Exists(path))
            {
                certificate = new X509Certificate2(path, config.Data.Client.SSL.Password, X509KeyStorageFlags.Exportable);
            }
        }

        public async Task<ITunnelConnection> RelayAsync(RelayInfo relayInfo)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                //问一下能不能中继
                RelayAskResultInfo relayAskResultInfo = await RelayAsk(relayInfo);
                relayInfo.FlowingId = relayAskResultInfo.FlowingId;
                if (relayInfo.FlowingId == 0 || relayAskResultInfo.Nodes.Count == 0)
                {
                    return null;
                }

                //连接中继节点服务器
                Socket socket = await ConnectNodeServer(relayInfo, relayAskResultInfo.Nodes);
                if (socket == null)
                {
                    return null;
                }

                //让对方确认中继
                if (await RelayConfirm(relayInfo) == false)
                {
                    return null;
                }

                //成功建立连接，
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
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return null;
        }

        private async Task<RelayAskResultInfo> RelayAsk(RelayInfo relayInfo)
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
                return new RelayAskResultInfo();
            }
            return MemoryPackSerializer.Deserialize<RelayAskResultInfo>(resp.Data.Span);
        }
        private async Task<bool> RelayConfirm(RelayInfo relayInfo)
        {
            //通知对方去确认中继
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward,
                Payload = MemoryPackSerializer.Serialize(relayInfo),
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        private async Task<Socket> ConnectNodeServer(RelayInfo relayInfo, List<RelayNodeReportInfo> nodes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);

            try
            {
                foreach (var node in nodes)
                {
                    //连接中继服务器
                    Socket socket = new Socket(node.EndPoint.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                    socket.KeepAlive();
                    await socket.ConnectAsync(node.EndPoint).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                    //是否允许连接
                    int length = await socket.ReceiveAsync(buffer);
                    if (buffer[0] != 0)
                    {
                        socket.SafeClose();
                        return null;
                    }

                    //建立关联
                    RelayMessage relayMessage = new RelayMessage { FlowId = relayInfo.FlowingId, Type = RelayMessengerType.Ask, FromId = relayInfo.FromMachineId, ToId = relayInfo.RemoteMachineId };
                    await socket.SendAsync(relayMessage.ToBytes());


                    relayInfo.Server = node.EndPoint;
                    relayInfo.NodeId = node.Id;

                    return socket;
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
                if (relayInfo.Server == null)
                    relayInfo.Server = clientSignInState.Connection.Address;
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
                        LoggerHelper.Instance.Error($"need ssl");
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

        public async Task<int> RelayTestAsync(RelayTestInfo relayTestInfo)
        {
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

                return resp.Code == MessageResponeCodes.OK ? (int)sw.ElapsedMilliseconds : -1;
            }
            catch (Exception)
            {
            }
            return -1;
        }
    }
}
