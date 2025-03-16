
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
using linker.messenger.signin;
using System.Text;
using System;

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
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;

        public RelayClientTransportSelfHost(IMessengerSender messengerSender, ISerializer serializer, IRelayClientStore relayClientStore, SignInClientState signInClientState, IMessengerStore messengerStore)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.relayClientStore = relayClientStore;
            this.signInClientState = signInClientState;
            this.messengerStore = messengerStore;
        }

        public async Task<ITunnelConnection> RelayAsync(RelayInfo170 relayInfo)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                //问一下能不能中继
                RelayAskResultInfo170 relayAskResultInfo = await RelayAsk(relayInfo).ConfigureAwait(false);
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
                Socket socket = await ConnectNodeServer(relayInfo, relayAskResultInfo.Nodes).ConfigureAwait(false);
                if (socket == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay connect server fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
                    return null;
                }

                //让对方确认中继
                if (await RelayConfirm(relayInfo).ConfigureAwait(false) == false)
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
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
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
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
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

        private async Task<RelayAskResultInfo170> RelayAsk(RelayInfo170 relayInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.RelayAsk170,
                Payload = serializer.Serialize(relayInfo),
                Timeout = 2000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return new RelayAskResultInfo170();
            }

            RelayAskResultInfo170 result = serializer.Deserialize<RelayAskResultInfo170>(resp.Data.Span);

            return result;

        }
        private async Task<Socket> ConnectNodeServer(RelayInfo170 relayInfo, List<RelayServerNodeReportInfo170> nodes)
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
                            ep = signInClientState.Connection.Address;
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
                        await socket.SendAsync(new byte[] { (byte)ResolverType.Relay }).ConfigureAwait(false);
                        await socket.SendAsync(serializer.Serialize(relayMessage)).ConfigureAwait(false);

                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG) LoggerHelper.Instance.Debug($"relay  connected {ep}");

                        //是否允许连接
                        int length = await socket.ReceiveAsync(buffer.AsMemory(0, 1)).ConfigureAwait(false);

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
        private async Task<bool> RelayConfirm(RelayInfo170 relayInfo)
        {
            //通知对方去确认中继
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward170,
                Payload = serializer.Serialize(relayInfo),
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<bool> OnBeginAsync(RelayInfo170 relayInfo, Action<ITunnelConnection> callback)
        {
            try
            {
                IPEndPoint ep = relayInfo.Server == null || relayInfo.Server.Address.Equals(IPAddress.Any) ? signInClientState.Connection.Address : relayInfo.Server;

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
                await socket.SendAsync(new byte[] { (byte)ResolverType.Relay }).ConfigureAwait(false);
                await socket.SendAsync(serializer.Serialize(relayMessage)).ConfigureAwait(false);

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

        private async Task<TunnelConnectionTcp> WaitSSL(Socket socket, RelayInfo170 relayInfo)
        {
            try
            {
                SslStream sslStream = null;
                if (relayInfo.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false);
                    await sslStream.AuthenticateAsServerAsync(messengerStore.Certificate, false, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false).ConfigureAwait(false);
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
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
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

        public async Task<List<RelayServerNodeReportInfo170>> RelayTestAsync(RelayTestInfo170 relayTestInfo)
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayTest170,
                    Payload = serializer.Serialize(relayTestInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<RelayServerNodeReportInfo170>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayServerNodeReportInfo170>();
        }
    }


    public class RelayClientTransportSelfHostUdp : IRelayClientTransport
    {
        public string Name => "LinkerUdp";
        public RelayClientType Type => RelayClientType.Linker;
        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;

        private byte[] relayFlag = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.relay.flag");

        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly IRelayClientStore relayClientStore;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;

        public RelayClientTransportSelfHostUdp(IMessengerSender messengerSender, ISerializer serializer, IRelayClientStore relayClientStore, SignInClientState signInClientState, IMessengerStore messengerStore)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.relayClientStore = relayClientStore;
            this.signInClientState = signInClientState;
            this.messengerStore = messengerStore;
        }

        public async Task<ITunnelConnection> RelayAsync(RelayInfo170 relayInfo)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                //问一下能不能中继
                RelayAskResultInfo170 relayAskResultInfo = await RelayAsk(relayInfo).ConfigureAwait(false);
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
                var (socket, ep) = await ConnectNodeServer(relayInfo, relayAskResultInfo.Nodes).ConfigureAwait(false);
                if (socket == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay connect server fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
                    return null;
                }

                //让对方确认中继
                if (await RelayConfirm(relayInfo).ConfigureAwait(false) == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay confirm fail,flowid:{relayInfo.FlowingId},nodes:{relayAskResultInfo.Nodes.Count}");
                    return null;
                }

                return new TunnelConnectionUdp
                {
                    UdpClient = socket,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(ep),
                    TransactionId = relayInfo.TransactionId,
                    RemoteMachineId = relayInfo.RemoteMachineId,
                    RemoteMachineName = relayInfo.RemoteMachineName,
                    TransportName = Name,
                    Direction = TunnelDirection.Forward,
                    ProtocolType = ProtocolType,
                    Type = TunnelType.Relay,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                    Receive = true,
                    SSL = relayInfo.SSL,
                    Crypto = CryptoFactory.CreateSymmetric(relayInfo.RemoteMachineId)
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

        private async Task<RelayAskResultInfo170> RelayAsk(RelayInfo170 relayInfo)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.RelayAsk170,
                Payload = serializer.Serialize(relayInfo),
                Timeout = 2000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return new RelayAskResultInfo170();
            }

            RelayAskResultInfo170 result = serializer.Deserialize<RelayAskResultInfo170>(resp.Data.Span);

            return result;

        }
        private async Task<(Socket, IPEndPoint)> ConnectNodeServer(RelayInfo170 relayInfo, List<RelayServerNodeReportInfo170> nodes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4 * 1024);

            try
            {
                foreach (var node in nodes.Where(c => c.Id == relayInfo.NodeId).Concat(nodes.Where(c => c.Id != relayInfo.NodeId)))
                {
                    try
                    {
                        IPEndPoint ep = node.EndPoint;
                        if (ep == null || ep.Address.Equals(IPAddress.Any))
                        {
                            ep = signInClientState.Connection.Address;
                        }

                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"connect relay server {ep}");

                        //连接中继服务器
                        Socket socket = new Socket(ep.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();

                        //建立关联
                        RelayMessageInfo relayMessage = new RelayMessageInfo
                        {
                            FlowId = relayInfo.FlowingId,
                            Type = RelayMessengerType.Ask,
                            FromId = relayInfo.FromMachineId,
                            ToId = relayInfo.RemoteMachineId,
                            NodeId = node.Id,
                        };
                        await socket.SendToAsync(BuildPacket(buffer, relayMessage), ep).ConfigureAwait(false);
                        
                        //是否允许连接
                        IPEndPoint temp = new IPEndPoint(IPAddress.Any, 0);
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, temp).ConfigureAwait(false);
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay  connected {ep}->{buffer[0]}");
                        if (buffer[0] == 0)
                        {
                            relayInfo.Server = node.EndPoint;
                            relayInfo.NodeId = node.Id;
                            return (socket, ep);
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
            return (null, null);
        }
        private Memory<byte> BuildPacket(byte[] buffer,RelayMessageInfo relayMessage)
        {
            int index = 0;
            buffer[0] = (byte)ResolverType.Relay;
            buffer[1] = (byte)RelayUdpStep.Connect;
            buffer[2] = (byte)relayFlag.Length;
            index += 3;

            relayFlag.AsMemory().CopyTo(buffer.AsMemory(index));
            index += relayFlag.Length;

            byte[] bytes = serializer.Serialize(relayMessage);
            bytes.AsMemory().CopyTo(buffer.AsMemory(index));
            index += bytes.Length;

            return buffer.AsMemory(0, index);
        }

        private async Task<bool> RelayConfirm(RelayInfo170 relayInfo)
        {
            //通知对方去确认中继
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.RelayForward170,
                Payload = serializer.Serialize(relayInfo),
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> OnBeginAsync(RelayInfo170 relayInfo, Action<ITunnelConnection> callback)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                IPEndPoint ep = relayInfo.Server == null || relayInfo.Server.Address.Equals(IPAddress.Any) ? signInClientState.Connection.Address : relayInfo.Server;
                Socket socket = new Socket(ep.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                socket.WindowsUdpBug();

                RelayMessageInfo relayMessage = new RelayMessageInfo
                {
                    FlowId = relayInfo.FlowingId,
                    Type = RelayMessengerType.Answer,
                    FromId = relayInfo.FromMachineId,
                    ToId = relayInfo.RemoteMachineId,
                    NodeId = relayInfo.NodeId,
                };
                await socket.SendToAsync(BuildPacket(buffer, relayMessage), ep).ConfigureAwait(false);

                callback(new TunnelConnectionUdp
                {
                    UdpClient = socket,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(ep),
                    TransactionId = relayInfo.TransactionId,
                    RemoteMachineId = relayInfo.RemoteMachineId,
                    RemoteMachineName = relayInfo.RemoteMachineName,
                    TransportName = Name,
                    Direction = TunnelDirection.Forward,
                    ProtocolType = ProtocolType,
                    Type = TunnelType.Relay,
                    Mode = TunnelMode.Server,
                    Label = string.Empty,
                    Receive = true,
                    SSL = relayInfo.SSL,
                    Crypto = CryptoFactory.CreateSymmetric(relayInfo.RemoteMachineId)
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
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return false;
        }

        public async Task<List<RelayServerNodeReportInfo170>> RelayTestAsync(RelayTestInfo170 relayTestInfo)
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayTest170,
                    Payload = serializer.Serialize(relayTestInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<RelayServerNodeReportInfo170>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayServerNodeReportInfo170>();
        }
    }
}
