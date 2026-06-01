using linker.libs.extends;
using linker.libs.web;
using linker.tunnel;
using linker.tunnel.connection;

namespace linker.messenger.pcp
{
    public sealed class PcpApiController : IApiController
    {
        private readonly TunnelTransfer tunnelTransfer;
        public PcpApiController(TunnelTransfer tunnelTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;
        }
        public bool Connect(ApiControllerParamsInfo param)
        {
            PcpConnectInfo info = param.Content.DeJson<PcpConnectInfo>();
            info.Configures["flag"] = "pcp";
            info.Configures["pcp"] = new PcpInfo { NodeId = info.NodeId }.ToJson();
            _ = tunnelTransfer.ConnectAsync(info.ToMachineId, info.TransactionId, info.Configures, tunnelTypes: [TunnelType.PCP]);
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
