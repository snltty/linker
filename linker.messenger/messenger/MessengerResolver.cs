using linker.libs;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using linker.libs.extends;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using linker.messenger.resolver;
using System.IO;

namespace linker.messenger.messenger
{
    /// <summary>
    /// 消息处理总线
    /// </summary>
    public class MessengerResolver : IConnectionReceiveCallback, IResolver
    {
        public ResolverType Type => ResolverType.Messenger;

        delegate void VoidDelegate(IMessengerConnection connection);
        delegate Task TaskDelegate(IMessengerConnection connection);

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly MessengerSender messengerSender;

        private X509Certificate serverCertificate;
        public MessengerResolver(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
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

        public virtual void OnSendt(ushort id, ulong length)
        {

        }
        public virtual void OnReceived(ushort id, ulong length)
        {

        }

        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            try
            {
                NetworkStream networkStream = new NetworkStream(socket, false);
                SslStream sslStream = new SslStream(networkStream, true);
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13, false).ConfigureAwait(false);
                IMessengerConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

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

        public async Task<IMessengerConnection> BeginReceiveClient(Socket socket)
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
                IMessengerConnection connection = CreateConnection(sslStream, networkStream, socket, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);

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
        private IMessengerConnection CreateConnection(SslStream stream, NetworkStream networkStream, Socket socket, IPEndPoint local, IPEndPoint remote)
        {
            return new TcpMessengerConnection(stream, networkStream, socket, local, remote)
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

            foreach (IMessenger messenger in list)
            {
                Type type = messenger.GetType();

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
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
        public async Task Receive(IMessengerConnection connection, ReadOnlyMemory<byte> data, object state)
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
                OnReceived(requestWrap.MessengerId, (ulong)data.Length);
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