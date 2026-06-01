using linker.libs;
using linker.messenger.node;
using linker.messenger.reverse.messenger;

namespace linker.messenger.reverse.server
{
    /// <summary>
    /// 穿透节点操作
    /// </summary>
    public class ReverseServerNodeTransfer : NodeTransfer<ReverseServerConfigInfo, ReverseServerNodeStoreInfo, ReverseServerNodeReportInfo>
    {
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;

        private readonly ReverseServerConnectionTransfer ReverseServerConnectionTransfer;
        public ReverseServerNodeTransfer(ISerializer serializer, IMessengerSender messengerSender, ReverseServerConnectionTransfer ReverseServerConnectionTransfer,
            ICommonStore commonStore, IReverseNodeConfigStore nodeConfigStore,
            ReverseServerNodeReportTransfer ReverseServerNodeReportTransfer)
            : base(commonStore, nodeConfigStore, ReverseServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.ReverseServerConnectionTransfer = ReverseServerConnectionTransfer;
        }
        public async Task<bool> ProxyForward(ReverseProxyInfo info)
        {
            if (ReverseServerConnectionTransfer.TryGet(ConnectionSideType.Master, info.NodeId, out var connection))
            {
                return await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.ProxyForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return false;
        }
        public async Task<List<string>> Heart(List<string> ids, string masterNodeId)
        {
            if (ReverseServerConnectionTransfer.TryGet(ConnectionSideType.Master, masterNodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.Heart,
                    Payload = serializer.Serialize(ids)
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<string>>(resp.Data.Span);
                }

            }

            return [];
        }
    }

}
