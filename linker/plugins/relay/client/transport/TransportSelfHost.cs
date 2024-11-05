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
using System.Buffers;
using linker.plugins.relay.server;
using linker.plugins.resolver;
using System.Net.NetworkInformation;

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

                //测试一下延迟
                if (relayAskResultInfo.Nodes.Count > 1)
                {
                    //relayAskResultInfo.Nodes = await TestDelay(relayAskResultInfo.Nodes);
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
        private async Task<List<RelayNodeReportInfo>> TestDelay(List<RelayNodeReportInfo> list)
        {
            //测试前几个就行了
            List<RelayNodeReportInfo> result = list.Take(10).ToList();

            Dictionary<string, RelayNodeDelayInfo> delays = result.ToDictionary(c => c.Id, d => new RelayNodeDelayInfo
            {
                Delay = 65535,
                Id = d.Id,
                IP = d.EndPoint == null || d.EndPoint.Address.Equals(IPAddress.Any) ? clientSignInState.Connection.Address.Address : d.EndPoint.Address
            });

            //让对面测一测
            Task<MessageResponeInfo> respTask = messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)RelayMessengerIds.NodeDelayForward,
                Payload = MemoryPackSerializer.Serialize(delays),
                Timeout = 5000
            });
            //自己测一测
            var tasks = delays.Select(async (c) =>
            {
                using Ping ping = new Ping();
                var resp = await ping.SendPingAsync(c.Value.IP, 1000);
                c.Value.Delay = resp.Status == IPStatus.Success ? (int)resp.RoundtripTime : 65535;
            });
            await Task.WhenAll(tasks);
            MessageResponeInfo resp = await respTask;

            //两边的延迟加起来，看哪个服务器更快
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                Dictionary<string, RelayNodeDelayInfo> remotes = MemoryPackSerializer.Deserialize<Dictionary<string, RelayNodeDelayInfo>>(resp.Data.Span);
                foreach (var item in result)
                {
                    if (delays.TryGetValue(item.Id, out RelayNodeDelayInfo local) && remotes.TryGetValue(item.Id, out RelayNodeDelayInfo remote))
                    {
                        item.Delay = local.Delay + remote.Delay;
                    }
                }
                return result.OrderByDescending(c => c.LastTicks)
                    //带宽倒序
                    .OrderByDescending(c => c.MaxBandwidth)
                    //最大连接数倒序
                    .OrderByDescending(c => c.MaxConnection)
                    //连接数比例升序
                    .OrderBy(c => c.ConnectionRatio)
                    //延迟升序
                    .OrderBy(c => c.Delay).ToList();
            }

            return result;
        }
        private async Task<Socket> ConnectNodeServer(RelayInfo relayInfo, List<RelayNodeReportInfo> nodes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);

            try
            {
                if (string.IsNullOrWhiteSpace(relayInfo.NodeId) == false)
                {
                    nodes = nodes.Where(c => c.Id == relayInfo.NodeId).ToList();
                }

                foreach (var node in nodes)
                {
                    IPEndPoint ep = node.EndPoint;
                    if (ep == null || ep.Address.Equals(IPAddress.Any))
                    {
                        ep = clientSignInState.Connection.Address;
                    }

                    //连接中继服务器
                    Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                    socket.KeepAlive();
                    await socket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                    //建立关联
                    RelayMessage relayMessage = new RelayMessage
                    {
                        FlowId = relayInfo.FlowingId,
                        Type = RelayMessengerType.Ask,
                        FromId = relayInfo.FromMachineId,
                        ToId = relayInfo.RemoteMachineId,
                        NodeId = node.Id,
                    };
                    await socket.SendAsync(new byte[] { (byte)ResolverType.Relay });
                    await socket.SendAsync(MemoryPackSerializer.Serialize(relayMessage));

                    //是否允许连接
                    int length = await socket.ReceiveAsync(buffer);
                    if (buffer[0] != 0)
                    {
                        socket.SafeClose();
                        return null;
                    }

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


        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<bool> OnBeginAsync(RelayInfo relayInfo, Action<ITunnelConnection> callback)
        {
            try
            {
                IPEndPoint ep = relayInfo.Server == null || relayInfo.Server.Address.Equals(IPAddress.Any) ? clientSignInState.Connection.Address : relayInfo.Server;

                Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();
                await socket.ConnectAsync(ep).WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                RelayMessage relayMessage = new RelayMessage
                {
                    FlowId = relayInfo.FlowingId,
                    Type = RelayMessengerType.Answer,
                    FromId = relayInfo.FromMachineId,
                    ToId = relayInfo.RemoteMachineId,
                    NodeId = relayInfo.NodeId,
                };
                await socket.SendAsync(new byte[] { (byte)ResolverType.Relay });
                await socket.SendAsync(MemoryPackSerializer.Serialize(relayMessage));

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

        public async Task<List<RelayNodeReportInfo>> RelayTestAsync(RelayTestInfo relayTestInfo)
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.RelayTest,
                    Payload = MemoryPackSerializer.Serialize(relayTestInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return MemoryPackSerializer.Deserialize<List<RelayNodeReportInfo>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayNodeReportInfo>();
        }
    }
}
