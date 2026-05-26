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
            _ = tunnelTransfer.ConnectAsync(info.ToMachineId, info.TransactionId, TunnelProtocolType.Udp,
               configures: new() { ["flag"] = "pcp", ["pcp"] = new PcpInfo { NodeId = info.NodeId }.ToJson() }, tunnelTypes: [TunnelType.PCP]);
            return true;
        }

    }


    public sealed class PcpConnectInfo
    {
        public string ToMachineId { get; set; }
        public string TransactionId { get; set; }
        public string NodeId { get; set; }
    }
}
