using linker.libs;
using linker.libs.extends;
using linker.messenger.node;
using linker.messenger.reverse.messenger;
using linker.messenger.signin;

namespace linker.messenger.reverse.server
{
    /// <summary>
    /// 穿透主机操作
    /// </summary>
    public class ReverseServerMasterTransfer
    {
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;
        private readonly IReverseServerWhiteListStore ReverseServerWhiteListStore;
        private readonly ReverseServerConnectionTransfer ReverseServerConnectionTransfer;
        private readonly ReverseServerNodeReportTransfer ReverseServerNodeReportTransfer;


        public ReverseServerMasterTransfer(ISerializer serializer, IMessengerSender messengerSender, IReverseServerWhiteListStore ReverseServerWhiteListStore,
            ReverseServerConnectionTransfer ReverseServerConnectionTransfer, ReverseServerNodeReportTransfer ReverseServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.ReverseServerWhiteListStore = ReverseServerWhiteListStore;
            this.ReverseServerConnectionTransfer = ReverseServerConnectionTransfer;
            this.ReverseServerNodeReportTransfer = ReverseServerNodeReportTransfer;
        }

        public async Task<ReverseAddResultInfo> Start(ReverseAddInfo info, SignCacheInfo from)
        {
            if (string.IsNullOrWhiteSpace(info.NodeId))
            {
                var nodes = await ReverseServerNodeReportTransfer.GetNodes(from.Super, from.UserId, from.MachineId);
                if (nodes.Count > 0)
                {
                    info.NodeId = nodes[0].NodeId;
                }
            }
            var node = await ReverseServerNodeReportTransfer.GetNode(info.NodeId).ConfigureAwait(false);
            if (node == null)
            {
                return new ReverseAddResultInfo
                {
                    BufferSize = 1,
                    Message = "node not found",
                    Success = false
                };
            }

            List<NodeWhiteListInfo> Reverse = await ReverseServerWhiteListStore.GetNodes(from.UserId, from.MachineId);
            string target = string.IsNullOrWhiteSpace(info.Domain) ? info.RemotePort.ToString() : info.Domain;
            info.Super = from.Super;

            var bandwidth = Reverse.Where(c => (c.Nodes.Contains($"sfp->{target}") || c.Nodes.Contains($"sfp->*")) && (c.Nodes.Contains(info.NodeId) || c.Nodes.Contains($"*"))).ToList();
            if (bandwidth.Any(c => c.Bandwidth < 0))
            {
                return new ReverseAddResultInfo
                {
                    BufferSize = 1,
                    Message = "white list deny",
                    Success = false
                };
            }

            info.Bandwidth = bandwidth.Count > 0
                ? bandwidth.Any(c => c.Bandwidth == 0) ? 0 : bandwidth.Max(c => c.Bandwidth)
                : info.Super ? 0 : node.Bandwidth;


            if (ReverseServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = ReverseServerNodeReportTransfer.Config.NodeId;
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.Start,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<ReverseAddResultInfo>(resp.Data.Span);
                }
            }
            return new ReverseAddResultInfo
            {
                BufferSize = 1,
                Message = "node connection not found",
                Success = false
            };
        }
        public async Task<ReverseAddResultInfo> Stop(ReverseAddInfo info)
        {
            if (ReverseServerConnectionTransfer.TryGet(ConnectionSideType.Node, info.NodeId, out var connection))
            {
                info.NodeId = ReverseServerNodeReportTransfer.Config.NodeId;
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)ReverseMessengerIds.Stop,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<ReverseAddResultInfo>(resp.Data.Span);
                }
            }
            return new ReverseAddResultInfo
            {
                BufferSize = 1,
                Message = "node connection not found",
                Success = false
            };
        }



    }
}
