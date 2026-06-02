
using linker.libs.extends;
using linker.libs.web;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpApiController : IApiController
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly PcpNodeTransfer pcpHistoryTransfer;

        public PcpApiController(TunnelTransfer tunnelTransfer, PcpNodeTransfer pcpHistoryTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.pcpHistoryTransfer = pcpHistoryTransfer;
        }
        public bool Connect(ApiControllerParamsInfo param)
        {
            PcpConnectInfo info = param.Content.DeJson<PcpConnectInfo>();
            info.Configures["flag"] = "pcp";
            info.Configures["pcp"] = new PcpInfo { NodeId = info.NodeId }.ToJson();
            _ = tunnelTransfer.ConnectAsync(info.ToMachineId, info.TransactionId, info.Configures, tunnelTypes: [TunnelType.PCP]);
            return true;
        }
        public async Task<List<PcpNodeInfo>> GetNodes(ApiControllerParamsInfo param)
        {
            return await pcpHistoryTransfer.GetNodes(param.Content, string.Empty).ConfigureAwait(false);
        }
        public bool DelNodes(ApiControllerParamsInfo param)
        {
            pcpHistoryTransfer.RemoveNodes(param.Content.DeJson<List<string>>());
            return true;
        }

    }

    public sealed class PcpConnectInfo
    {
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
        public Dictionary<string, string> Configures { get; set; } = [];
    }
}
