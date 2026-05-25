using linker.libs.extends;
using linker.libs.web;

namespace linker.messenger.pcp
{
    public sealed class PcpApiController : IApiController
    {
        public PcpApiController(PcpTransfer pcpTransfer)
        {
            this.pcpTransfer = pcpTransfer;
        }

        private readonly PcpTransfer pcpTransfer;

        public bool Connect(ApiControllerParamsInfo param)
        {
            PcpConnectInfo info = param.Content.DeJson<PcpConnectInfo>();
            _ = pcpTransfer.ConnectAsync(info.ToMachineId, info.TransactionId);
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
