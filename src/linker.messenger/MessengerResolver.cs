using linker.libs;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.libs.extends;
using System.Buffers;

namespace linker.messenger
{
    /// <summary>
    /// 信标消息处理总线
    /// </summary>
    public interface IMessengerResolver
    {
        public Task<IConnection> BeginReceiveClient(Socket socket);
        public Task<IConnection> BeginReceiveClient(Socket socket, bool sendFlag, byte flag, Memory<byte> data);
        public void AddMessenger(List<IMessenger> list);
        public Task BeginReceiveServer(Socket socket, Memory<byte> memory);
        public Task BeginReceiveServer(Socket socket, IPEndPoint ep, Memory<byte> memory);
    }

    public class MessengerResolverResolver : IResolver
    {
        public byte Type => (byte)ResolverType.Messenger;

        private readonly IMessengerResolver messengerResolver;
        public MessengerResolverResolver(IMessengerResolver messengerResolver)
        {
            this.messengerResolver = messengerResolver;
        }
        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            await messengerResolver.BeginReceiveServer(socket, memory).ConfigureAwait(false);
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await messengerResolver.BeginReceiveServer(socket, ep, memory).ConfigureAwait(false);
        }
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
        private readonly IMessengerStore messengerStore;
        public MessengerResolver(IMessengerSender messengerSender, IMessengerStore messengerStore)
        {
            this.messengerSender = messengerSender;
            this.messengerStore = messengerStore;
        }


        public virtual void Add(ushort id, long receiveBytes, long sendtBytes) { }
        public virtual void AddStopwatch(ushort id, long time, MessageTypes type) { }

        /// <summary>
        /// 以服务器模式接收数据 TCP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async Task BeginReceiveServer(Socket socket, Memory<byte> memory)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
            NetworkStream networkStream = new NetworkStream(socket, false);
            SslStream sslStream = new SslStream(networkStream, true, ValidateServerCertificate, null);
            try
            {
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = messengerStore.Certificate,
                    ClientCertificateRequired = OperatingSystem.IsAndroid(),
                    EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, cts.Token).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时
                IConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

                connection.BeginReceive(this, null, true);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                socket.SafeClose();
                sslStream.Dispose();
                networkStream.Dispose();
            }
        }
        /// <summary>
        /// 以服务器模式接收数据 UDP
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ep"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async Task BeginReceiveServer(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// 以客户端模式接收数据
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public async Task<IConnection> BeginReceiveClient(Socket socket)
        {
            return await BeginReceiveClient(socket, false, 0, Helper.EmptyArray).ConfigureAwait(false);
        }
        /// <summary>
        /// 以客户端模式接收数据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="sendFlag"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public async Task<IConnection> BeginReceiveClient(Socket socket, bool sendFlag, byte flag, Memory<byte> data)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(16);
            buffer.Memory.Span[0] = flag;
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                if (sendFlag)
                {
                    await socket.SendAsync(buffer.Memory.Slice(0, 1)).ConfigureAwait(false);
                }
                if (data.Length > 0)
                {
                    await socket.SendAsync(data).ConfigureAwait(false);
                }

                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
                NetworkStream networkStream = new NetworkStream(socket, false);
                SslStream sslStream = new SslStream(networkStream, true, ValidateServerCertificate, null);
                try
                {
#pragma warning disable SYSLIB0039 // 类型或成员已过时
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        AllowRenegotiation = true,
                        EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        ClientCertificates = new X509CertificateCollection { messengerStore.Certificate }
                    }, cts.Token).ConfigureAwait(false);
#pragma warning restore SYSLIB0039 // 类型或成员已过时

                    IConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);
                    connection.BeginReceive(this, null, true);
                    return connection;
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error(ex);
                    socket.SafeClose();
                    sslStream.Dispose();
                    networkStream.Dispose();

                    return null;
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);

                socket.SafeClose();
            }
            return null;
        }
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private static TcpConnection CreateConnection(SslStream stream, NetworkStream networkStream, Socket socket, IPEndPoint local, IPEndPoint remote)
        {
            return new TcpConnection(stream, networkStream, socket, local, remote)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        /// <summary>
        /// 添加信标
        /// </summary>
        public void AddMessenger(List<IMessenger> list)
        {
            Type voidType = typeof(void);
            Type midType = typeof(MessengerIdAttribute);

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add messenger {string.Join(",", list.Select(c => c.GetType().Name))}");

            foreach (IMessenger messenger in list.Distinct())
            {

                Type type = messenger.GetType();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
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
        /// 处理消息，不需要调用，内部会处理
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task Receive(IConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            MessageResponseWrap responseWrap = connection.ReceiveResponseWrap;
            MessageRequestWrap requestWrap = connection.ReceiveRequestWrap;
            long start = Environment.TickCount64;
            try
            {
                //回复的消息
                if ((MessageTypes)(data.Span[0] & 0b0000_1111) == MessageTypes.RESPONSE)
                {
                    responseWrap.FromArray(data);
                    ushort messengerId = messengerSender.Response(responseWrap);
                    AddStopwatch(messengerId, Environment.TickCount64 - start, MessageTypes.RESPONSE);
                    return;
                }

                //新的请求
                requestWrap.FromArray(data);
                Add(requestWrap.MessengerId, data.Length, 0);
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
                AddStopwatch(requestWrap.MessengerId, Environment.TickCount64 - start, MessageTypes.REQUEST);
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