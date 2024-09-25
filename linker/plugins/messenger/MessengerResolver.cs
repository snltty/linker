using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.libs.extends;
using linker.plugins.resolver;
using MemoryPack;
using linker.plugins.flow;

namespace linker.plugins.messenger
{
    /// <summary>
    /// 消息处理总线
    /// </summary>
    public sealed class MessengerResolver : IConnectionReceiveCallback, IResolver, IFlow
    {
        public ResolverType Type => ResolverType.Messenger;

        delegate void VoidDelegate(IConnection connection);
        delegate Task TaskDelegate(IConnection connection);

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly MessengerSender messengerSender;
        private readonly ServiceProvider serviceProvider;

        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "Messenger";
        private Dictionary<ushort, FlowItemInfo> messangerFlows { get; } = new Dictionary<ushort, FlowItemInfo>();



        private X509Certificate serverCertificate;
        public MessengerResolver(MessengerSender messengerSender, ServiceProvider serviceProvider)
        {
            this.messengerSender = messengerSender;
            this.serviceProvider = serviceProvider;
        }

        public void Init(string certificate, string password)
        {
            string path = Path.GetFullPath(certificate);
            if (File.Exists(path))
            {
                serverCertificate = new X509Certificate(path, password);
            }
            else
            {
                LoggerHelper.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
        }

        public async Task Resolve(Socket socket)
        {
            try
            {
                NetworkStream networkStream = new NetworkStream(socket, false);
                SslStream sslStream = new SslStream(networkStream, true);
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
                IConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

                connection.BeginReceive(this, null, true);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }

        public async Task<IConnection> BeginReceiveClient(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                await socket.SendAsync(new byte[] { (byte)ResolverType.Messenger }).ConfigureAwait(false);
                NetworkStream networkStream = new NetworkStream(socket, false);
                SslStream sslStream = new SslStream(networkStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    AllowRenegotiation = true,
                    EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13
                }).ConfigureAwait(false);
                IConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

                connection.BeginReceive(this, null, true);

                return connection;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private IConnection CreateConnection(SslStream stream, NetworkStream networkStream, Socket socket, IPEndPoint local, IPEndPoint remote)
        {
            return new TcpConnection(stream, networkStream, socket, local, remote)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        /// <summary>
        /// 加载所有消息处理器
        /// </summary>
        /// <param name="assemblys"></param>
        public void LoadMessenger(Assembly[] assemblys)
        {
            Type voidType = typeof(void);
            Type midType = typeof(MessengerIdAttribute);
            var types = ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)).Distinct();

            foreach (Type type in types)
            {
                object obj = serviceProvider.GetService(type);
                if (obj == null)
                {
                    continue;
                }
                LoggerHelper.Instance.Info($"load messenger:{type.Name}");

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    MessengerIdAttribute mid = method.GetCustomAttribute(midType) as MessengerIdAttribute;
                    if (mid != null)
                    {
                        if (messengers.ContainsKey(mid.Id) == false)
                        {
                            MessengerCacheInfo cache = new MessengerCacheInfo
                            {
                                Target = obj
                            };
                            //void方法
                            if (method.ReturnType == voidType)
                            {
                                cache.VoidMethod = (VoidDelegate)Delegate.CreateDelegate(typeof(VoidDelegate), obj, method);
                            }
                            //异步方法
                            else if (method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null)
                            {
                                cache.TaskMethod = (TaskDelegate)Delegate.CreateDelegate(typeof(TaskDelegate), obj, method);
                            }
                            messengers.TryAdd(mid.Id, cache);

                            messangerFlows.TryAdd(mid.Id, new FlowItemInfo { });
                        }
                        else
                        {
                            LoggerHelper.Instance.Warning($"{type.Name}->{method.Name}->{mid.Id} 消息id已存在");
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task Receive(IConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            MessageResponseWrap responseWrap = connection.ReceiveResponseWrap;
            MessageRequestWrap requestWrap = connection.ReceiveRequestWrap;
            try
            {

                //回复的消息
                if ((MessageTypes)(data.Span[0] & 0b0000_1111) == MessageTypes.RESPONSE)
                {
                    responseWrap.FromArray(data);
                    messengerSender.Response(responseWrap);
                    return;
                }

                //新的请求
                requestWrap.FromArray(data);
                //404,没这个插件
                if (messengers.TryGetValue(requestWrap.MessengerId, out MessengerCacheInfo plugin) == false)
                {
                    if (requestWrap.Reply == true)
                    {
                        bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.NOT_FOUND,
                            RequestId = requestWrap.RequestId
                        }).ConfigureAwait(false);
                    }
                    return;
                }

                //流量统计
                if (messangerFlows.TryGetValue(requestWrap.MessengerId, out FlowItemInfo messengerFlowItemInfo))
                {
                    ReceiveBytes += (ulong)data.Length;
                    messengerFlowItemInfo.ReceiveBytes += (ulong)data.Length;
                }

                if (plugin.VoidMethod != null)
                {
                    plugin.VoidMethod(connection);
                }
                else if (plugin.TaskMethod != null)
                {
                    await plugin.TaskMethod(connection).ConfigureAwait(false);
                }
                //有需要回复的
                if (requestWrap.Reply == true && connection.ResponseData.Length > 0)
                {
                    //流量统计
                    if (messengerFlowItemInfo != null)
                    {
                        SendtBytes += (ulong)connection.ResponseData.Length;
                        messengerFlowItemInfo.SendtBytes += (ulong)connection.ResponseData.Length;
                    }
                    bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = connection.ResponseData,
                        RequestId = requestWrap.RequestId
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                connection.Return();
            }
        }

        public Dictionary<ushort, FlowItemInfo> GetFlows()
        {
            return messangerFlows;
        }

        /// <summary>
        /// 消息插件缓存
        /// </summary>
        private struct MessengerCacheInfo
        {
            /// <summary>
            /// 对象
            /// </summary>
            public object Target { get; set; }
            /// <summary>
            /// 空返回方法
            /// </summary>
            public VoidDelegate VoidMethod { get; set; }
            /// <summary>
            /// Task返回方法
            /// </summary>
            public TaskDelegate TaskMethod { get; set; }
        }

    }


    [MemoryPackable]
    public sealed partial class MessengerFlowItemInfo
    {
        public ulong ReceiveBytes { get; set; }
        public ulong SendtBytes { get; set; }
    }
}