
using linker.messenger.relay.messenger;
using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Buffers;
using linker.messenger.relay.server;

namespace linker.messenger.relay.client.transport
{
    /// <summary>
    /// linker中继
    /// </summary>
    public class RelayClientTransportSelfHost : IRelayClientTransport
    {
        public string Name => "Linker";
        public RelayClientType Type => RelayClientType.Linker;
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly IRelayClientStore relayClientStore;
        public RelayClientTransportSelfHost(IMessengerSender messengerSender, ISerializer serializer, IRelayClientStore relayClientStore)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.relayClientStore = relayClientStore;
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
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay ask fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
                    return null;
                }

                //测试一下延迟
                if (relayAskResultInfo.Nodes.Count > 1)
                {
                    //relayAskResultInfo.Nodes = await TestDelay(relayAskResultInfo.Nodes);
                }

                //连接中继节点服务器
                Socket socket = await ConnectNodeServer(relayInfo, relayAskResultInfo.Nodes);
                if (socket == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay connect server fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
                    return null;
                }

                //让对方确认中继
                if (await RelayConfirm(relayInfo) == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay confirm fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
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
                    NodeId = relayInfo.NodeId,
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
                Connection = relayClientStore.SigninConnection,
                MessengerId = (ushort)RelayMessengerIds.RelayAsk,
                Payload = serializer.Serialize(relayInfo),
                Timeout = 2000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return new RelayAskResultInfo();
            }

            RelayAskResultInfo result = serializer.Deserialize<RelayAskResultInfo>(resp.Data.Span);

            return result;

        }
        private async Task<Socket> ConnectNodeServer(RelayInfo relayInfo, List<RelayServerNodeReportInfo> nodes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);

            try
            {
                foreach (var node in nodes.Where(c => c.Id == relayInfo.NodeId).Concat(nodes.Where(c => c.Id != relayInfo.NodeId)))
                {
                    try
                    {
                        IPEndPoint ep = node.EndPoint;
                        if (ep == null || ep.Address.Equals(IPAddress.Any))
                        {
                            ep = relayClientStore.SigninConnection.Address;
                        }

                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"connect relay server {ep}");

                        //连接中继服务器
                        Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                        socket.KeepAlive();
                        await socket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                        //建立关联
                        RelayMessageInfo relayMessage = new RelayMessageInfo
                        {
                            FlowId = relayInfo.FlowingId,
                            Type = RelayMessengerType.Ask,
                            FromId = relayInfo.FromMachineId,
                            ToId = relayInfo.RemoteMachineId,
                            NodeId = node.Id,
                        };
                        if (relayClientStore.Flag > 0)
                            await socket.SendAsync(new byte[] { relayClientStore.Flag });
                        await socket.SendAsync(serializer.Serialize(relayMessage));

                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"relay  connected {ep}");

                        //是否允许连接
                        int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1));

                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay  connected {ep}->{buffer[0]}");
                        if (buffer[0] == 0)
                        {
                            relayInfo.Server = node.EndPoint;
                            relayInfo.NodeId = node.Id;
                            return socket;
                        }
                        socket.SafeClose();
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    }
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
        private async Task<bool> RelayConfirm(RelayInfo relayInfo)
        {
            //通知对方去确认中继
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = relayClientStore.SigninConnection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward,
                Payload = serializer.Serialize(relayInfo),
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<bool> OnBeginAsync(RelayInfo relayInfo, Action<ITunnelConnection> callback)
        {
            try
            {
                IPEndPoint ep = relayInfo.Server == null || relayInfo.Server.Address.Equals(IPAddress.Any) ? relayClientStore.SigninConnection.Address : relayInfo.Server;

                Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                RelayMessageInfo relayMessage = new RelayMessageInfo
                {
                    FlowId = relayInfo.FlowingId,
                    Type = RelayMessengerType.Answer,
                    FromId = relayInfo.FromMachineId,
                    ToId = relayInfo.RemoteMachineId,
                    NodeId = relayInfo.NodeId,
                };
                if (relayClientStore.Flag > 0)
                    await socket.SendAsync(new byte[] { relayClientStore.Flag });
                await socket.SendAsync(serializer.Serialize(relayMessage));

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
                    sslStream = new SslStream(new NetworkStream(socket, false), false);
                    await sslStream.AuthenticateAsServerAsync(relayClientStore.Certificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
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
                    NodeId = relayInfo.NodeId,
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

        public async Task<List<RelayServerNodeReportInfo>> RelayTestAsync(RelayTestInfo relayTestInfo)
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = relayClientStore.SigninConnection,
                    MessengerId = (ushort)RelayMessengerIds.RelayTest,
                    Payload = serializer.Serialize(relayTestInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<RelayServerNodeReportInfo>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayServerNodeReportInfo>();
        }
    }
}
