using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Buffers;
using linker.libs.extends;

namespace linker.plugins.messenger
{
    /// <summary>
    /// 消息处理总线
    /// </summary>
    public sealed class MessengerResolver : IConnectionReceiveCallback
    {
        delegate void VoidDelegate(IConnection connection);
        delegate Task TaskDelegate(IConnection connection);

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly MessengerSender messengerSender;
        private readonly ServiceProvider serviceProvider;

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
        public async Task BeginReceiveServer(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }
                socket.KeepAlive();

                if (await ReceiveType(socket).ConfigureAwait(false) == 0)
                {
                    return;
                }

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
        public async Task<IConnection> BeginReceiveClient(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                await socket.SendAsync(new byte[] { 1 }).ConfigureAwait(false);
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
        public Memory<byte> BuildSendData(byte[] data, IPEndPoint ep)
        {
            //给客户端返回他的IP+端口
            data[0] = (byte)ep.AddressFamily;
            ep.Address.TryWriteBytes(data.AsSpan(1), out int length);
            ((ushort)ep.Port).ToBytes(data.AsMemory(1 + length));

            //防止一些网关修改掉它的外网IP
            for (int i = 0; i < 1 + length + 2; i++)
            {
                data[i] = (byte)(data[i] ^ byte.MaxValue);
            }
            return data.AsMemory(0, 1 + length + 2);
        }
        private async Task<byte> ReceiveType(Socket socket)
        {
            byte[] sendData = ArrayPool<byte>.Shared.Rent(20);
            try
            {
                await socket.ReceiveAsync(sendData.AsMemory(0, 1), SocketFlags.None).ConfigureAwait(false);
                byte type = sendData[0];
                if (type == 0)
                {
                    Memory<byte> memory = BuildSendData(sendData, socket.RemoteEndPoint as IPEndPoint);
                    await socket.SendAsync(memory, SocketFlags.None).ConfigureAwait(false);
                }
                return type;
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendData);
            }
            return 1;
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