using linker.libs;
using linker.libs.extends;
using linker.messenger;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.transport
{
    public class TransportRelay : ITunnelTransport
    {
        public string Name => "TcpRelay";

        public string Label => "TCP、服务器中继";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Other;

        public bool Reverse => false;

        public bool DisableReverse => true;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 0;

        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };

        private readonly ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;

        public TransportRelay(IMessengerSender messengerSender, ISerializer serializer, SignInClientState signInClientState, IMessengerStore messengerStore, ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientState = signInClientState;
            this.messengerStore = messengerStore;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        private X509Certificate certificate;
        public void SetSSL(X509Certificate certificate)
        {
            this.certificate = certificate;
        }

        public virtual async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                //问一下能不能中继
                RelayAskResultInfo ask = await RelayAsk(tunnelTransportInfo).ConfigureAwait(false);
                List<RelayServerNodeStoreInfo> nodes = ask.Nodes;
                if (ask.Nodes.Count == 0)
                {
                    throw new Exception("relay client ask fail,no relay nodes");
                }

                //连接中继节点服务器
                Socket socket = await ConnectNodeServer(tunnelTransportInfo, ask).ConfigureAwait(false);
                if (socket == null)
                {
                    throw new Exception("relay client connect node server fail");
                }
                tunnelTransportInfo.TransactionTag = ask.Info.ToJson();

                //让对方确认中继
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    throw new Exception("relay client begin fail");
                }

                //成功建立连接，
                SslStream sslStream = null;
                if (tunnelTransportInfo.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false, ValidateServerCertificate, null);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        ClientCertificates = new X509CertificateCollection { messengerStore.Certificate }
                    }).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                }

                await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);

                return new TunnelConnectionTcp
                {
                    Direction = TunnelDirection.Forward,
                    ProtocolType = TunnelProtocolType.Tcp,
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    Stream = sslStream,
                    Socket = socket,
                    Mode = TunnelMode.Client,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
                    TransactionId = tunnelTransportInfo.TransactionId,
                    TransportName = Name,
                    Type = TunnelType.Relay,
                    NodeId = ask.Info.NodeId,
                    SSL = tunnelTransportInfo.SSL,
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
            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        private async Task<RelayAskResultInfo> RelayAsk(TunnelTransportInfo tunnelTransportInfo)
        {
            RelayInfo relayInfo = new RelayInfo();
            try
            {
                relayInfo = tunnelTransportInfo.TransactionTag.DeJson<RelayInfo>();
            }
            catch (Exception)
            {
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)RelayMessengerIds.Ask,
                Payload = serializer.Serialize((tunnelTransportInfo.Remote.MachineId, tunnelTransportInfo.TransactionId, tunnelTransportInfo.FlowId)),
                Timeout = 2000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return new RelayAskResultInfo { Info = relayInfo, Nodes = new List<RelayServerNodeStoreInfo>() };
            }
            RelayAskResultInfo ask = serializer.Deserialize<RelayAskResultInfo>(resp.Data.Span);
            ask.Info = relayInfo;
            ask.Info.MasterId = ask.MasterId;

            return ask;

        }
        private async Task<Socket> ConnectNodeServer(TunnelTransportInfo tunnelTransportInfo, RelayAskResultInfo ask)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);

            try
            {
                foreach (var node in ask.Nodes.Where(c => c.NodeId == ask.Info.NodeId).Concat(ask.Nodes.Where(c => c.NodeId != ask.Info.NodeId)))
                {
                    try
                    {
                        IPEndPoint ep = NetworkHelper.GetEndPoint(node.Host, 1802);
                        if (ep == null || ep.Address.Equals(IPAddress.Any) || ep.Address.Equals(IPAddress.Loopback))
                        {
                            ep = signInClientState.Connection.Address;
                        }
                        Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                        socket.KeepAlive();
                        socket.IPv6Only(ep.AddressFamily, false);
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Debug($"relay client connect server {ep}");

                        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
                        try
                        {
                            //连接中继服务器
                            await socket.ConnectAsync(ep, cts.Token).ConfigureAwait(false);
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            {
                                LoggerHelper.Instance.Debug($"relay client connected {ep}");
                            }

                            //建立关联
                            RelayMessageInfo relayMessage = new RelayMessageInfo
                            {
                                FlowId = tunnelTransportInfo.FlowId,
                                Type = RelayMessengerType.Ask,
                                FromId = tunnelTransportInfo.Local.MachineId,
                                ToId = tunnelTransportInfo.Remote.MachineId,
                                MasterId = ask.MasterId,
                            };
                            if (await SendMessage(socket, relayMessage).ConfigureAwait(false))
                            {
                                ask.Info.Node = ep;
                                ask.Info.NodeId = node.NodeId;
                                return socket;
                            }
                        }
                        catch (Exception)
                        {
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
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private async Task<bool> SendMessage(Socket socket, RelayMessageInfo relayMessage)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                byte[] sendBytes = crypto.Encode(serializer.Serialize(relayMessage));

                IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(sendBytes.Length + 5);

                buffer.Memory.Span[0] = (byte)ResolverType.Relay;
                sendBytes.Length.ToBytes(buffer.Memory.Slice(1));

                sendBytes.CopyTo(buffer.Memory.Slice(5));

                await socket.SendAsync(buffer.Memory.Slice(0, sendBytes.Length + 5)).ConfigureAwait(false);

                int length = await socket.ReceiveAsync(buffer.Memory.Slice(0, 1), cts.Token).ConfigureAwait(false);

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($" relay SendMessage recv {length}->{buffer.Memory.Span[0]}");
                }
                return length == 1 && buffer.Memory.Slice(0, 1).Span.SequenceEqual(Helper.TrueArray);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            return false;
        }

        public virtual async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            try
            {
                if (tunnelTransportInfo.SSL && certificate == null)
                {
                    LoggerHelper.Instance.Error($"relay client {Name}->ssl Certificate not found");
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                    return;
                }

                RelayInfo relayInfo = tunnelTransportInfo.TransactionTag.DeJson<RelayInfo>();

                IPEndPoint ep = relayInfo.Node == null || relayInfo.Node.Address.Equals(IPAddress.Any) || relayInfo.Node.Address.Equals(IPAddress.Loopback) ? signInClientState.Connection.Address : relayInfo.Node;
                Socket socket = new Socket(ep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.KeepAlive();

                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
                try
                {
                    await socket.ConnectAsync(ep, cts.Token).ConfigureAwait(false);
                    RelayMessageInfo relayMessage = new RelayMessageInfo
                    {
                        FlowId = tunnelTransportInfo.FlowId,
                        Type = RelayMessengerType.Answer,
                        FromId = tunnelTransportInfo.Local.MachineId,
                        ToId = tunnelTransportInfo.Remote.MachineId,
                        MasterId = relayInfo.MasterId,
                    };
                    if (await SendMessage(socket, relayMessage).ConfigureAwait(false))
                    {
                        ITunnelConnection connection = await WaitSSL(socket, tunnelTransportInfo, relayInfo);
                        OnConnected(connection);
                        await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"relay client connect server {ep} {ex}");
                    }
                    socket.SafeClose();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            OnConnected(null);
            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
        }
        private async Task<TunnelConnectionTcp> WaitSSL(Socket socket, TunnelTransportInfo tunnelTransportInfo, RelayInfo relayInfo)
        {
            try
            {
                SslStream sslStream = null;
                if (tunnelTransportInfo.SSL)
                {
                    sslStream = new SslStream(new NetworkStream(socket, false), false, ValidateServerCertificate, null);
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                    await sslStream.AuthenticateAsServerAsync(messengerStore.Certificate, OperatingSystem.IsAndroid(), SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                }
                return new TunnelConnectionTcp
                {
                    Direction = TunnelDirection.Reverse,
                    ProtocolType = TunnelProtocolType.Tcp,
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    Stream = sslStream,
                    Socket = socket,
                    Mode = TunnelMode.Server,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(socket.RemoteEndPoint as IPEndPoint),
                    TransactionId = tunnelTransportInfo.TransactionId,
                    TransportName = Name,
                    Type = TunnelType.Relay,
                    NodeId = relayInfo.NodeId,
                    SSL = tunnelTransportInfo.SSL,
                    BufferSize = 3,
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error($"relay client wait ssl {ex}");
                }
                socket.SafeClose();
            }
            return null;
        }


        public virtual void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
        }
        public virtual void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
        }

        public async Task<List<RelayServerNodeStoreInfo>> RelayTestAsync()
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Nodes,
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<RelayServerNodeStoreInfo>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayServerNodeStoreInfo>();
        }
    }

    /// <summary>
    /// 中继交换数据
    /// </summary>
    public partial class RelayInfo
    {
        public string NodeId { get; set; }
        public string MasterId { get; set; }
        public IPEndPoint Node { get; set; }
    }
    public partial class RelayAskResultInfo
    {
        public RelayInfo Info { get; set; }
        public string MasterId { get; set; }
        public List<RelayServerNodeStoreInfo> Nodes { get; set; } = new List<RelayServerNodeStoreInfo>();
    }
    public sealed partial class RelayMessageInfo
    {
        public RelayMessengerType Type { get; set; }
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string MasterId { get; set; }
    }
}
