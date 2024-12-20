using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.relay.server.caching;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;
using linker.messenger.tunnel;
using linker.plugins.tunnel;
using linker.tunnel.transport;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.example
{
    internal class Program
    {
        public static PublicConfigInfo publicConfigInfo = new PublicConfigInfo();

        static ISerializer serializer;
        static IMessengerSender messengerSender;
        static IMessengerResolver messengerResolver;

        static void Main(string[] args)
        {
            //序列化
            serializer = new Serializer();

            //信标发送和接受
            messengerSender = new MessengerSender();
            messengerResolver = new MessengerResolver(messengerSender);
            messengerResolver.Initialize(publicConfigInfo.Certificate);
        }

        static SignCaching signCaching;
        static SignInArgsTransfer signInArgsTransfer;
        static ISignInStore signInStore;

        static TunnelExternalResolver tunnelExternalResolver;
        static TunnelMessengerAdapter tunnelMessengerAdapter;
        static void Server()
        {

            //登录相关
            signInArgsTransfer = new SignInArgsTransfer();
            //signInArgsTransfer.LoadArgs(new List<ISignInArgs>());
            signInStore = new SignInStore();
            signCaching = new SignCaching(signInStore, signInArgsTransfer);
            SignInServerMessenger signInServerMessenger = new SignInServerMessenger(messengerSender, signCaching, serializer);

            //打洞相关
            tunnelExternalResolver = new TunnelExternalResolver();
            TunnelServerMessenger tunnelServerMessenger = new TunnelServerMessenger(messengerSender, signCaching, serializer);

            //中继相关
            IRelayServerMasterStore relayServerMasterStore = new RelayServerMasterStore();
            IRelayServerNodeStore relayServerNodeStore = new RelayServerNodeStore();
            RelayServerNodeTransfer relayServerNodeTransfer = new RelayServerNodeTransfer(serializer, relayServerNodeStore, relayServerMasterStore);
            RelayServerResolver relayServerResolver = new RelayServerResolver(relayServerNodeTransfer, serializer);
            IRelayServerCaching relayServerCaching =  new RelayServerCachingMemory(serializer);
            RelayServerMasterTransfer relayServerMasterTransfer = new RelayServerMasterTransfer(relayServerCaching, serializer, relayServerMasterStore);
            RelayServerReportResolver relayServerReportResolver = new RelayServerReportResolver(relayServerMasterTransfer);
            //自定义中继验证
            RelayServerValidatorTransfer relayServerValidatorTransfer = new RelayServerValidatorTransfer();
            //relayServerValidatorTransfer.LoadValidators(new List<IRelayServerValidator> { });
            RelayServerMessenger relayServerMessenger = new RelayServerMessenger(messengerSender, signCaching, serializer, relayServerMasterTransfer, relayServerValidatorTransfer);

            //加载这些信标处理器
            messengerResolver.LoadMessenger(new List<IMessenger>
            {
                signInServerMessenger,
                tunnelServerMessenger,
                relayServerMessenger
            });

            //TCP
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, publicConfigInfo.Port));
            socket.Listen(int.MaxValue);
            TimerHelper.Async(async () =>
            {
                while (true)
                {
                    var client = await socket.AcceptAsync();
                    TimerHelper.Async(async () =>
                    {
                        var bytes = new byte[1024];
                        int length = await client.ReceiveAsync(bytes.AsMemory(0, 1));
                        //信标
                        if (bytes[0] == 0)
                        {
                            await messengerResolver.BeginReceiveServer(socket, Helper.EmptyArray);
                        }
                        //外网端口
                        else if (bytes[0] == 1)
                        {
                            await tunnelExternalResolver.Resolve(client, Helper.EmptyArray);
                        }
                        //中继节点报告
                        else if (bytes[0] == 2)
                        {
                            await relayServerReportResolver.Resolve(client, Helper.EmptyArray);
                        }
                        //中继
                        else if (bytes[0] == 3)
                        {
                            await relayServerResolver.Resolve(client, Helper.EmptyArray);
                        }

                    });
                }
            });

            //UDP
            Socket socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.Bind(new IPEndPoint(IPAddress.Any, publicConfigInfo.Port));
            socketUdp.WindowsUdpBug();
            TimerHelper.Async(async () =>
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] buffer = new byte[1 * 1024 * 1024];
                while (true)
                {
                    try
                    {
                        SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer, SocketFlags.None, endPoint).ConfigureAwait(false);
                        IPEndPoint ep = result.RemoteEndPoint as IPEndPoint;
                        try
                        {
                            await tunnelExternalResolver.Resolve(socketUdp, ep, buffer.AsMemory(0, result.ReceivedBytes));
                        }
                        catch (Exception)
                        {
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            });
        }
    }

    public sealed class PublicConfigInfo
    {
        public string MachineId { get; set; }
        public IConnection SignConnection { get; set; }
        public IPAddress[] LocalIps { get; set; }
        public int RouteLevel { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public List<TunnelTransportItemInfo> TunnelTransports { get; set; }


        public int Port { get; set; } = 12345;
    }

    /// <summary>
    /// 中继节点信息存储库
    /// </summary>
    public sealed class RelayServerNodeStore : IRelayServerNodeStore
    {
        public byte Flag => 2;

        public int ServicePort => Program.publicConfigInfo.Port;

        public RelayServerNodeInfo Node => new RelayServerNodeInfo { };

        public void Confirm()
        {
        }

        public void SetMaxGbTotalLastBytes(ulong value)
        {
        }

        public void SetMaxGbTotalMonth(int month)
        {
        }
    }
    /// <summary>
    /// 中继主机信息存储库
    /// </summary>
    public sealed class RelayServerMasterStore : IRelayServerMasterStore
    {
        public RelayServerMasterInfo Master => new RelayServerMasterInfo { SecretKey = "snltty" };
    }

    /// <summary>
    /// 自定义打洞的存储库
    /// </summary>
    public sealed class TunnelMessengerAdapterStore : ITunnelMessengerAdapterStore
    {
        public IConnection SignConnection => Program.publicConfigInfo.SignConnection;

        public NetworkInfo Network => new NetworkInfo { MachineId = Program.publicConfigInfo.MachineId, LocalIps = Program.publicConfigInfo.LocalIps, RouteLevel = Program.publicConfigInfo.RouteLevel };

        public X509Certificate2 Certificate => Program.publicConfigInfo.Certificate;

        public List<TunnelTransportItemInfo> TunnelTransports => Program.publicConfigInfo.TunnelTransports;

        public TunnelMessengerAdapterStore()
        {
        }
        public bool SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            return true;
        }
    }

    /// <summary>
    /// 自定义序列化
    /// </summary>
    public sealed class Serializer : ISerializer
    {
        public T Deserialize<T>(ReadOnlySpan<byte> buffer)
        {
            return buffer.GetString().DeJson<T>();
        }

        public byte[] Serialize<T>(T value)
        {
            return value.ToJson().ToBytes();
        }
    }
    /// <summary>
    /// 自定义登录持久化存储
    /// </summary>
    public sealed class SignInStore : ISignInStore
    {
        public void Confirm()
        {
        }

        public bool Delete(string id)
        {
            return true;
        }

        public SignCacheInfo Find(string id)
        {
            return null;
        }

        public IEnumerable<SignCacheInfo> Find()
        {
            return new List<SignCacheInfo>();
        }

        public string Insert(SignCacheInfo value)
        {
            return string.Empty;
        }

        public string NewId()
        {
            return Guid.NewGuid().ToString();
        }

        public bool Update(SignCacheInfo value)
        {
            return true;
        }
    }


}
