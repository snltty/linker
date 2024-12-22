using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.relay.server.caching;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;
using linker.messenger.tunnel;
using linker.plugins.tunnel;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace linker.messenger.example
{
    internal class Program
    {
        public static PublicConfigInfo publicConfigInfo = new PublicConfigInfo
        {
            Certificate = new X509Certificate2("./snltty.pfx", "oeq9tw1o"),
            TunnelTransports = new List<TunnelTransportItemInfo>
            {
                new TunnelTransportItemInfo{ BufferSize=3, Disabled=false, DisableReverse=false, DisableSSL=false, Name="udp", Order=0, ProtocolType= TunnelProtocolType.Udp.ToString(), Reverse=true, SSL=true },
                new TunnelTransportItemInfo{ BufferSize=3, Disabled=false, DisableReverse=false, DisableSSL=false, Name="TcpP2PNAT", Order=1, ProtocolType= TunnelProtocolType.Tcp.ToString(), Reverse=true, SSL=true },
                new TunnelTransportItemInfo{ BufferSize=3, Disabled=false, DisableReverse=false, DisableSSL=false, Name="TcpNutssb", Order=2, ProtocolType= TunnelProtocolType.Tcp.ToString(), Reverse=true, SSL=true },
            }
        };

        static async Task Main(string[] args)
        {
            LoggerConsole();

            Console.WriteLine($"输入server 或者 client:");
            string type = Console.ReadLine();

            if (type == "server")
            {
                Server();
            }
            else
            {
                await Client();
            }

            Console.ReadLine();
        }

        public sealed class TunnelConnectionReceiveCallback : ITunnelConnectionReceiveCallback
        {
            public async Task Closed(ITunnelConnection connection, object state)
            {
                Console.WriteLine($"{(connection.Type == TunnelType.P2P ? "打洞" : "中继")}关闭，收到 {connection.IPEndPoint} {connection.RemoteMachineId} 的关闭");
                await Task.CompletedTask;
            }

            public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state)
            {
                Console.WriteLine($"{(connection.Type == TunnelType.P2P ? "打洞" : "中继")}数据，收到 {connection.IPEndPoint} {connection.RemoteMachineId} 的数据:{data.Span.GetString()}");
                await Task.CompletedTask;
            }
        }
        private static async Task Client()
        {
            //序列化
            ISerializer serializer = new Serializer();

            //信标发送和接受
            IMessengerSender messengerSender = new MessengerSender();
            IMessengerResolver messengerResolver = new MessengerResolver(messengerSender);

            //打洞相关
            TunnelExcludeIPTransfer tunnelExcludeIPTransfer = new TunnelExcludeIPTransfer();
            //tunnelExcludeIPTransfer.LoadTunnelExcludeIPs(new List<ITunnelExcludeIP>());
            TunnelMessengerAdapter tunnelMessengerAdapter = new TunnelMessengerAdapter(messengerSender, tunnelExcludeIPTransfer, serializer, new TunnelMessengerAdapterStore());
            TunnelTransfer tunnelTransfer = new TunnelTransfer(tunnelMessengerAdapter);
            tunnelTransfer.SetConnectedCallback("default", (connection) =>
            {
                Console.WriteLine($"打洞成功，收到 {connection.IPEndPoint} {connection.RemoteMachineId} 的连接");
                connection.BeginReceive(new TunnelConnectionReceiveCallback(), null);
            });
            TunnelClientMessenger tunnelClientMessenger = new TunnelClientMessenger(tunnelTransfer, messengerSender, serializer);


            //中继相关
            IRelayClientStore relayClientStore = new RelayClientStore();
            RelayClientTransfer relayClientTransfer = new RelayClientTransfer(messengerSender, serializer, relayClientStore);
            relayClientTransfer.SetConnectedCallback("default", (connection) =>
            {
                Console.WriteLine($"中继成功，收到 {connection.IPEndPoint} {connection.RemoteMachineId} 的连接");
                connection.BeginReceive(new TunnelConnectionReceiveCallback(), null);
            });
            RelayClientMessenger relayClientMessenger = new RelayClientMessenger(relayClientTransfer, serializer);

            //加载这些信标处理器
            messengerResolver.LoadMessenger(new List<IMessenger>
            {
                tunnelClientMessenger,
                relayClientMessenger
            });


            Console.WriteLine($"输入服务端ip端口:");
            publicConfigInfo.Host = Console.ReadLine();

            Console.WriteLine($"开始连接服务器");
            IPEndPoint server = NetworkHelper.GetEndPoint(publicConfigInfo.Host, 1802);
            Socket socket = new Socket(server.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            await socket.ConnectAsync(server).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
            publicConfigInfo.SignConnection = await messengerResolver.BeginReceiveClient(socket, true, 1).ConfigureAwait(false);

            Console.WriteLine($"开始登录");
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = publicConfigInfo.SignConnection,
                MessengerId = (ushort)SignInMessengerIds.SignIn_V_1_3_1,
                Timeout = 2000,
                Payload = serializer.Serialize(new SignInfo
                {
                    MachineName = Dns.GetHostName(),
                    MachineId = string.Empty,
                    Version = VersionHelper.version,
                    Args = new Dictionary<string, string>(),
                    GroupId = "default"
                })
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                Console.WriteLine($"sign in fail : {resp.Code}");
                publicConfigInfo.SignConnection?.Disponse(6);
                return;
            }

            SignInResponseInfo signResp = serializer.Deserialize<string>(resp.Data.Span).DeJson<SignInResponseInfo>();
            if (signResp.Status == false)
            {
                Console.WriteLine($"sign in fail : {signResp.Msg}");
                publicConfigInfo.SignConnection?.Disponse(6);
                return;
            }
            publicConfigInfo.SignConnection.Id = signResp.MachineId;
            Console.WriteLine($"你的id:{signResp.MachineId}");
            tunnelTransfer.Refresh();

            Console.WriteLine($"去连接吗?，1打洞，2中继:");
            string connect = Console.ReadLine();

            Console.WriteLine($"输入对方id:");
            string id = Console.ReadLine();

            ITunnelConnection tunnelConnection = null;
            switch (connect)
            {
                case "1":
                    {
                        tunnelConnection = await tunnelTransfer.ConnectAsync(id, "default", TunnelProtocolType.None);
                    }
                    break;
                case "2":
                    {
                        tunnelConnection = await relayClientTransfer.ConnectAsync(publicConfigInfo.SignConnection.Id, id, "default");
                    }
                    break;
                default:
                    break;
            }
            if (tunnelConnection != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    string msg = $"hello {i}";
                    var msgBytes = msg.ToBytes();

                    //首部4字节存长度，剩下的才是真实数据
                    byte[] bytes = new byte[4 + msgBytes.Length];
                    msgBytes.Length.ToBytes(bytes);
                    msgBytes.AsMemory().CopyTo(bytes.AsMemory(4));

                    await tunnelConnection.SendAsync(bytes);
                    await Task.Delay(1000);
                }
            }
        }

        private static void Server()
        {
            Console.WriteLine($"输入服务端端口:");
            publicConfigInfo.Port = int.Parse(Console.ReadLine());

            //序列化
            ISerializer serializer = new Serializer();

            //信标发送和接受
            IMessengerSender messengerSender = new MessengerSender();
            IMessengerResolver messengerResolver = new MessengerResolver(messengerSender);
            messengerResolver.Initialize(publicConfigInfo.Certificate);

            //登录相关
            SignInArgsTransfer signInArgsTransfer = new SignInArgsTransfer();
            //signInArgsTransfer.LoadArgs(new List<ISignInArgs>());
            ISignInStore signInStore = new SignInStore();
            SignCaching signCaching = new SignCaching(signInStore, signInArgsTransfer);
            SignInServerMessenger signInServerMessenger = new SignInServerMessenger(messengerSender, signCaching, serializer);

            //打洞相关
            TunnelExternalResolver tunnelExternalResolver = new TunnelExternalResolver();
            TunnelServerMessenger tunnelServerMessenger = new TunnelServerMessenger(messengerSender, signCaching, serializer);

            //中继相关
            IRelayServerMasterStore relayServerMasterStore = new RelayServerMasterStore();
            IRelayServerNodeStore relayServerNodeStore = new RelayServerNodeStore();
            RelayServerNodeTransfer relayServerNodeTransfer = new RelayServerNodeTransfer(serializer, relayServerNodeStore, relayServerMasterStore);
            RelayServerResolver relayServerResolver = new RelayServerResolver(relayServerNodeTransfer, serializer);
            IRelayServerCaching relayServerCaching = new RelayServerCachingMemory(serializer);
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
                        try
                        {
                            var bytes = new byte[1024];
                            int length = await client.ReceiveAsync(bytes.AsMemory(0, 1));
                            //外网端口
                            if (bytes[0] == 0)
                            {
                                await tunnelExternalResolver.Resolve(client, Helper.EmptyArray);
                            }
                            //信标
                            else if (bytes[0] == 1)
                            {
                                await messengerResolver.BeginReceiveServer(client, Helper.EmptyArray);
                            }
                            //中继
                            else if (bytes[0] == 2)
                            {
                                await relayServerResolver.Resolve(client, Helper.EmptyArray);
                            }
                            //中继节点报告
                            else if (bytes[0] == 3)
                            {
                                await relayServerReportResolver.Resolve(client, Helper.EmptyArray);
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Instance.Error(ex);
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
                            //外网端口
                            if (buffer[0] == 0)
                            {
                                await tunnelExternalResolver.Resolve(socketUdp, ep, buffer.AsMemory(1, result.ReceivedBytes - 1));
                            }
                            //中继节点报告
                            else if (buffer[0] == 3)
                            {
                                await relayServerReportResolver.Resolve(socketUdp, ep, buffer.AsMemory(1, result.ReceivedBytes - 1));
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                        break;
                    }
                }
            });
        }

        private static void LoggerConsole()
        {
            LoggerHelper.Instance.OnLogger += (model) =>
            {
                ConsoleColor currentForeColor = Console.ForegroundColor;
                switch (model.Type)
                {
                    case LoggerTypes.DEBUG:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LoggerTypes.INFO:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggerTypes.WARNING:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggerTypes.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        break;
                }
                string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                Console.WriteLine(line);
                Console.ForegroundColor = currentForeColor;
            };
        }
    }

    public sealed class PublicConfigInfo
    {
        public IConnection SignConnection { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public List<TunnelTransportItemInfo> TunnelTransports { get; set; }

        public string Host { get; set; }
        public int Port { get; set; } = 12345;
    }

    /// <summary>
    /// 中继的客户端存储库
    /// </summary>
    public sealed class RelayClientStore : IRelayClientStore
    {
        public byte Flag => 2;

        public X509Certificate2 Certificate => Program.publicConfigInfo.Certificate;

        public IConnection SigninConnection => Program.publicConfigInfo.SignConnection;

        public string SecretKey => string.Empty;

        public bool Disabled => false;

        public bool SSL => true;

        public RelayClientType RelayType => RelayClientType.Linker;
    }

    /// <summary>
    /// 中继节点信息存储库
    /// </summary>
    public sealed class RelayServerNodeStore : IRelayServerNodeStore
    {
        public byte Flag => 3;

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
        public X509Certificate2 Certificate => Program.publicConfigInfo.Certificate;

        public int RouteLevelPlus => 0;
        public int PortMapPrivate => 0;
        public int PortMapPublic => 0;

        public TunnelMessengerAdapterStore()
        {
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports()
        {
            return await Task.FromResult(Program.publicConfigInfo.TunnelTransports);
        }
        public async Task<bool> SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            return await Task.FromResult(true);
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
