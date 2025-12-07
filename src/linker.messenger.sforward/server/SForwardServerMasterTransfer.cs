using linker.libs;
using linker.messenger.sforward.messenger;
using linker.messenger.signin;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 穿透主机操作
    /// </summary>
    public class SForwardServerMasterTransfer
    {
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly ISForwardServerWhiteListStore sForwardServerWhiteListStore;
        private readonly ISForwardServerNodeStore sForwardServerNodeStore;
        private readonly SForwardServerConnectionTransfer sForwardServerConnectionTransfer;
        private readonly SForwardServerNodeReportTransfer sForwardServerNodeReportTransfer;


        public SForwardServerMasterTransfer(ISerializer serializer, IMessengerSender messengerSender, ISForwardServerWhiteListStore sForwardServerWhiteListStore,
            ISForwardServerNodeStore sForwardServerNodeStore, SForwardServerConnectionTransfer sForwardServerConnectionTransfer, SForwardServerNodeReportTransfer sForwardServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.sForwardServerWhiteListStore = sForwardServerWhiteListStore;
            this.sForwardServerNodeStore = sForwardServerNodeStore;
            this.sForwardServerConnectionTransfer = sForwardServerConnectionTransfer;
            this.sForwardServerNodeReportTransfer = sForwardServerNodeReportTransfer;
        }

        public async Task<SForwardAddResultInfo> Start(SForwardAddInfo info, SignCacheInfo from)
        {
            if (string.IsNullOrWhiteSpace(info.NodeId))
            {
                var nodes = await sForwardServerNodeReportTransfer.GetNodes(from.Super, from.UserId, from.MachineId);
                if (nodes.Count > 0)
                {
                    info.NodeId = nodes[0].NodeId;
                }
            }
            var node = await sForwardServerNodeReportTransfer.GetNode(info.NodeId).ConfigureAwait(false);
            if (node == null)
            {
                return new SForwardAddResultInfo
                {
                    BufferSize = 1,
                    Message = "node not found",
                    Success = false
                };
            }

            List<SForwardWhiteListItem> sforward = await sForwardServerWhiteListStore.GetNodes(from.UserId, from.MachineId);
            string target = string.IsNullOrWhiteSpace(info.Domain) ? info.RemotePort.ToString() : info.Domain;
            info.Super = from.Super;

            var bandwidth = sforward.Where(c => (c.Nodes.Contains($"sfp->{target}") || c.Nodes.Contains($"sfp->*")) && (c.Nodes.Contains(info.NodeId) || c.Nodes.Contains($"*"))).ToList();
            if (bandwidth.Any(c => c.Bandwidth < 0))
            {
                return new SForwardAddResultInfo
                {
                    BufferSize = 1,
                    Message = "white list deny",
                    Success = false
                };
            }

            info.Bandwidth = bandwidth.Count > 0
                ? bandwidth.Any(c => c.Bandwidth == 0) ? 0 : bandwidth.Max(c => c.Bandwidth)
                : info.Super ? 0 : node.Bandwidth;


            if (sForwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, info.NodeId, out var connection))
            {
                info.NodeId = sForwardServerNodeReportTransfer.Config.NodeId;
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Start,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<SForwardAddResultInfo>(resp.Data.Span);
                }
            }
            return new SForwardAddResultInfo
            {
                BufferSize = 1,
                Message = "node not found",
                Success = false
            };
        }
        public async Task<SForwardAddResultInfo> Stop(SForwardAddInfo info)
        {
            if (sForwardServerConnectionTransfer.TryGet(ConnectionSideType.Master, info.NodeId, out var connection))
            {
                info.NodeId = sForwardServerNodeReportTransfer.Config.NodeId;
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)SForwardMessengerIds.Stop,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<SForwardAddResultInfo>(resp.Data.Span);
                }
            }
            return new SForwardAddResultInfo
            {
                BufferSize = 1,
                Message = "node not found",
                Success = false
            };
        }



    }
}
