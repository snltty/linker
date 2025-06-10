using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.plan
{
    /// <summary>
    /// 计划任务客户端
    /// </summary>
    public class PlanClientMessenger : IMessenger
    {
        private readonly PlanTransfer planTransfer;
        private readonly ISerializer serializer;
        public PlanClientMessenger(PlanTransfer planTransfer, ISerializer serializer)
        {
            this.planTransfer = planTransfer;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)PlanMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            PlanGetInfo info = serializer.Deserialize<PlanGetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            List<PlanInfo> result = planTransfer.Get(info.Category).ToList();
            connection.Write(serializer.Serialize(result));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)PlanMessengerIds.Add)]
        public void AddClient(IConnection connection)
        {
            PlanAddInfo info = serializer.Deserialize<PlanAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            planTransfer.Add(info.Plan);
            connection.Write(Helper.TrueArray);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)PlanMessengerIds.Remove)]
        public void RemoveClient(IConnection connection)
        {
            PlanRemoveInfo info = serializer.Deserialize<PlanRemoveInfo>(connection.ReceiveRequestWrap.Payload.Span);
            planTransfer.Remove(info.PlanId);
            connection.Write(Helper.TrueArray);
        }
    }

    /// <summary>
    /// 计划任务服务端
    /// </summary>
    public class PlanServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public PlanServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }
        [MessengerId((ushort)PlanMessengerIds.GetForward)]
        public async Task GetForward(IConnection connection)
        {
            PlanGetInfo info = serializer.Deserialize<PlanGetInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PlanMessengerIds.Get,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)PlanMessengerIds.GetForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        [MessengerId((ushort)PlanMessengerIds.AddForward)]
        public async Task AddForward(IConnection connection)
        {
            PlanAddInfo info = serializer.Deserialize<PlanAddInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PlanMessengerIds.Add,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)PlanMessengerIds.AddForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
        [MessengerId((ushort)PlanMessengerIds.RemoveForward)]
        public async Task RemoveForward(IConnection connection)
        {
            PlanRemoveInfo info = serializer.Deserialize<PlanRemoveInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)PlanMessengerIds.Remove,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)PlanMessengerIds.RemoveForward).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
    }
}
