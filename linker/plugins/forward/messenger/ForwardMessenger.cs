using linker.client.config;
using linker.plugins.messenger;
using linker.plugins.signin.messenger;
using MemoryPack;

namespace linker.plugins.forward.messenger
{
    public sealed class ForwardServerMessenger : IMessenger
    {

        private readonly IMessengerSender sender;
        private readonly SignCaching signCaching;

        public ForwardServerMessenger(IMessengerSender sender, SignCaching signCaching)
        {
            this.sender = sender;
            this.signCaching = signCaching;
        }

        /// <summary>
        /// 获取对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            string machineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
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
            ForwardAddForwardInfo info = MemoryPackSerializer.Deserialize<ForwardAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.AddClient,
                    Payload = MemoryPackSerializer.Serialize(info.Data)
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
            ForwardRemoveForwardInfo info = MemoryPackSerializer.Deserialize<ForwardRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.MachineId, out SignCacheInfo cacheTo) && signCaching.TryGet(connection.Id, out SignCacheInfo cacheFrom) && cacheFrom.GroupId == cacheTo.GroupId)
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = cacheTo.Connection,
                    MessengerId = (ushort)ForwardMessengerIds.RemoveClient,
                    Payload = MemoryPackSerializer.Serialize(info.Id)
                });
            }
        }

    }

    public sealed class ForwardClientMessenger : IMessenger
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly IMessengerSender sender;

        public ForwardClientMessenger(ForwardTransfer forwardTransfer, IMessengerSender sender)
        {
            this.forwardTransfer = forwardTransfer;
            this.sender = sender;
        }

        [MessengerId((ushort)ForwardMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(forwardTransfer.Get()));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.AddClient)]
        public void AddClient(IConnection connection)
        {
            ForwardInfo info = MemoryPackSerializer.Deserialize<ForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Add(info);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ForwardMessengerIds.RemoveClient)]
        public void RemoveClient(IConnection connection)
        {
            uint id = MemoryPackSerializer.Deserialize<uint>(connection.ReceiveRequestWrap.Payload.Span);
            forwardTransfer.Remove(id);
        }
    }

    [MemoryPackable]
    public sealed partial class ForwardAddForwardInfo
    {
        public string MachineId { get; set; }
        public ForwardInfo Data { get; set; }
    }
    [MemoryPackable]
    public sealed partial class ForwardRemoveForwardInfo
    {
        public string MachineId { get; set; }
        public uint Id { get; set; }
    }
}
