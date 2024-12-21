using linker.libs;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.libs.extends;

namespace linker.messenger
{
    /// <summary>
    /// 信标消息处理总线
    /// </summary>
    public interface IMessengerResolver
    {
        public void Initialize(string certificate, string password);
        public void Initialize(X509Certificate2 certificate);
        public Task<IConnection> BeginReceiveClient(Socket socket);
        public Task<IConnection> BeginReceiveClient(Socket socket, bool sendFlag, byte flag);
        public void LoadMessenger(List<IMessenger> list);
        public Task BeginReceiveServer(Socket socket, Memory<byte> memory);
        public Task BeginReceiveServer(Socket socket, IPEndPoint ep, Memory<byte> memory);
    }

    /// <summary>
    /// 消息处理总线
    /// </summary>
    public class MessengerResolver : IConnectionReceiveCallback, IMessengerResolver
    {
        delegate void VoidDelegate(IConnection connection);
        delegate Task TaskDelegate(IConnection connection);

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly IMessengerSender messengerSender;

        private X509Certificate2 serverCertificate;
        public MessengerResolver(IMessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public void Initialize(string certificate, string password)
        {
            string path = Path.GetFullPath(certificate);
            if (File.Exists(path))
            {
                serverCertificate = new X509Certificate2(path, password, X509KeyStorageFlags.Exportable);
            }
            else
            {
                LoggerHelper.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
        }
        public void Initialize(X509Certificate2 certificate)
        {
            serverCertificate = certificate;
        }

        public virtual void AddReceive(ushort id, ulong bytes) { }
        public virtual void AddSendt(ushort id, ulong bytes) { }

        public async Task BeginReceiveServer(Socket socket, Memory<byte> memory)
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
        public async Task BeginReceiveServer(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }

        public async Task<IConnection> BeginReceiveClient(Socket socket)
        {
            return await BeginReceiveClient(socket,false,0);
        }
        public async Task<IConnection> BeginReceiveClient(Socket socket,bool sendFlag,byte flag)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                if (sendFlag)
                {
                    await socket.SendAsync(new byte[] { flag }).ConfigureAwait(false);
                }
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
        public void LoadMessenger(List<IMessenger> list)
        {
            Type voidType = typeof(void);
            Type midType = typeof(MessengerIdAttribute);

            foreach (IMessenger messenger in list.Distinct())
            {

                Type type = messenger.GetType();
                foreach (var method in type.GetMethods( BindingFlags.Public | BindingFlags.Instance))
                {
                    MessengerIdAttribute mid = method.GetCustomAttribute(midType) as MessengerIdAttribute;
                    if (mid != null)
                    {
                        if (messengers.ContainsKey(mid.Id) == false)
                        {
                            MessengerCacheInfo cache = new MessengerCacheInfo
                            {
                                Target = messenger
                            };
                            //void方法
                            if (method.ReturnType == voidType)
                            {
                                cache.VoidMethod = (VoidDelegate)Delegate.CreateDelegate(typeof(VoidDelegate), messenger, method);
                            }
                            //异步方法
                            else if (method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null)
                            {
                                cache.TaskMethod = (TaskDelegate)Delegate.CreateDelegate(typeof(TaskDelegate), messenger, method);
                            }
                            messengers.TryAdd(mid.Id, cache);
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
                AddReceive(requestWrap.MessengerId, (ulong)data.Length);
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
                        }, requestWrap.MessengerId).ConfigureAwait(false);
                    }
                    return;
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
                    bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = connection.ResponseData,
                        RequestId = requestWrap.RequestId
                    }, requestWrap.MessengerId).ConfigureAwait(false);
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
}