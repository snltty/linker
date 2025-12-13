using linker.libs;
using linker.messenger.node;
using linker.messenger.sforward.messenger;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透节点操作
    /// </summary>
    public class SForwardServerNodeTransfer : NodeTransfer<SForwardServerConfigInfo, SForwardServerNodeStoreInfo, SForwardServerNodeReportInfo>
    {
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;

        private readonly SForwardServerConnectionTransfer sforwardServerConnectionTransfer;
        public SForwardServerNodeTransfer(ISerializer serializer, IMessengerSender messengerSender, SForwardServerConnectionTransfer sforwardServerConnectionTransfer,
            ICommonStore commonStore, ISForwardNodeConfigStore nodeConfigStore,
            SForwardServerNodeReportTransfer sforwardServerNodeReportTransfer)
            : base(commonStore, nodeConfigStore, sforwardServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.sforwardServerConnectionTransfer = sforwardServerConnectionTransfer;
        }
        public async Task<bool> ProxyNode(SForwardProxyInfo info)
        {
            if (sforwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, info.NodeId, out var connection))
            {
                return await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.ProxyForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return false;
        }
        public async Task<List<string>> Heart(List<string> ids, string masterNodeId)
        {
            if (sforwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, masterNodeId, out var connection))
            {
                var resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)SForwardMessengerIds.Heart,
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
