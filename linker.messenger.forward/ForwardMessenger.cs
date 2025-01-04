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
            if (signCaching.TryGet(machineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                sender.SendReply(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
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
        [MessengerId((ushort)ForwardMessengerIds.AddClientForward)]
        public async Task AddClientForward(IConnection connection)
        {
            ForwardAddForwardInfo info = serializer.Deserialize<ForwardAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.AddClient,
                    Payload = serializer.Serialize(info.Data)
                });
            }
        }
        /// <summary>
        /// 删除对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.RemoveClientForward)]
        public async Task RemoveClientForward(IConnection connection)
        {
            ForwardRemoveForwardInfo info = serializer.Deserialize<ForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.RemoveClient,
                    Payload = serializer.Serialize(info.Id)
                });
            }
        }


        [MessengerId((ushort)ForwardMessengerIds.TestClientForward)]
        public async Task TestClientForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(machineid, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.TestClientForward
                });
            }
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

        [MessengerId((ushort)ForwardMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            connection.Write(serializer.Serialize(forwardTransfer.Get()));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.AddClient)]
        public void AddClient(IConnection connection)
        {
            ForwardInfo info = serializer.Deserialize<ForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Add(info);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.RemoveClient)]
        public void RemoveClient(IConnection connection)
        {
            uint id = serializer.Deserialize<uint>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Remove(id);
        }


        [MessengerId((ushort)ForwardMessengerIds.TestClient)]
        public void TestClient(IConnection connection)
        {
            forwardTransfer.SubscribeTest();
        }
    }


}
