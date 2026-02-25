using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.forward
{
    public sealed class ForwardServerMessenger : IMessenger
    {

        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public ForwardServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id,machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.Get,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await sender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)ForwardMessengerIds.GetForward).ConfigureAwait(false);
                    }
                });
            }
        }
        /// <summary>
        /// 添加对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.AddForward)]
        public async Task AddForward(IConnection connection)
        {
            ForwardAddForwardInfo info = serializer.Deserialize<ForwardAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.Add,
                    Payload = serializer.Serialize(info.Data)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 删除对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.RemoveForward)]
        public async Task RemoveForward(IConnection connection)
        {
            ForwardRemoveForwardInfo info = serializer.Deserialize<ForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.Remove,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 订阅测试结果
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ForwardMessengerIds.SubTestForward)]
        public async Task SubTestForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id,machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.SubTest
                }).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// 测试服务
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.TestForward)]
        public void TestForward(IConnection connection)
        {
            Dictionary<string, List<ForwardTestInfo>> tests = serializer.Deserialize<Dictionary<string, List<ForwardTestInfo>>>(connection.ReceiveRequestWrap.Payload.Span);

            if(signCaching.TryGet(connection.Id, out SignCacheInfo from) == false)
            {
                return;
            }
            uint requiestid = connection.ReceiveRequestWrap.RequestId;

            var tasks = new List<TaskInfo>();
            foreach (var item in tests)
            {
                if (signCaching.TryGet(item.Key, out SignCacheInfo to) && from.SameGroup(to))
                {
                    tasks.Add(new TaskInfo
                    {
                        MachineId = item.Key,
                        Task = sender.SendReply(new MessageRequestWrap
                        {
                            Connection = to.Connection,
                            MessengerId = (ushort)ForwardMessengerIds.Test,
                            Payload = serializer.Serialize(item.Value),
                            Timeout = 3000
                        })
                    });
                }
            }
            Task.WhenAll(tasks.Select(c => c.Task)).ContinueWith(async (result) =>
            {
                var dic = tasks.Where(c => c.Task.Result.Code == MessageResponeCodes.OK && c.Task.Result.Data.Length > 0)
                 .ToDictionary(c => c.MachineId, d => serializer.Deserialize<List<ForwardTestInfo>>(d.Task.Result.Data.Span));

                await sender.ReplyOnly(new MessageResponseWrap
                {
                    RequestId = requiestid,
                    Connection = connection,
                    Payload = serializer.Serialize(dic)
                }, (ushort)ForwardMessengerIds.TestForward).ConfigureAwait(false);
            });
        }

        sealed class TaskInfo
        {
            public string MachineId { get; set; }
            public Task<MessageResponeInfo> Task { get; set; }
        }

    }

    public sealed class ForwardClientMessenger : IMessenger
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly IMessengerSender sender;
        private readonly ISerializer serializer;
        public ForwardClientMessenger(ForwardTransfer forwardTransfer, IMessengerSender sender, ISerializer serializer)
        {
            this.forwardTransfer = forwardTransfer;
            this.sender = sender;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取端口转发列表
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            connection.Write(serializer.Serialize(forwardTransfer.Get()));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            ForwardInfo info = serializer.Deserialize<ForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Add(info);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            int id = serializer.Deserialize<int>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Remove(id);
        }

        /// <summary>
        /// 订阅测试结果
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.SubTest)]
        public void SubTest(IConnection connection)
        {
            forwardTransfer.SubscribeTest();
        }

        /// <summary>
        /// 测试服务
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.Test)]
        public void Test(IConnection connection)
        {
            var list = serializer.Deserialize<List<ForwardTestInfo>>(connection.ReceiveRequestWrap.Payload.Span);
            uint requiestid = connection.ReceiveRequestWrap.RequestId;
            forwardTransfer.Test(list).ContinueWith(async (result) =>
            {
                if (result.Result)
                {
                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = serializer.Serialize(list)
                    }, (ushort)ForwardMessengerIds.Test).ConfigureAwait(false);
                }
                else
                {
                    await sender.ReplyOnly(new MessageResponseWrap
                    {
                        RequestId = requiestid,
                        Connection = connection,
                        Payload = Helper.EmptyArray
                    }, (ushort)ForwardMessengerIds.Test).ConfigureAwait(false);
                }

            });
        }
    }
}
