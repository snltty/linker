using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.server
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

        public MessengerResolver(MessengerSender messengerSender, ServiceProvider serviceProvider)
        {
            this.messengerSender = messengerSender;
            this.serviceProvider = serviceProvider;
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
                Logger.Instance.Warning($"load messenger:{type.Name}");

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
                            Logger.Instance.Error($"{type.Name}->{method.Name}->{mid.Id} 消息id已存在");
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
                    await plugin.TaskMethod(connection);
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
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
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