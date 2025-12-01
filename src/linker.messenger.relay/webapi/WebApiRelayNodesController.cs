using linker.libs.extends;
using linker.libs.web;
using linker.messenger.relay.server;

namespace linker.messenger.relay.webapi
{
    public sealed class WebApiRelayNodesController : IWebApiController
    {
        public string Path => "/relay/nodes.json";

        private readonly RelayServerNodeReportTransfer relayServerNodeReportTransfer;
        public WebApiRelayNodesController(RelayServerNodeReportTransfer relayServerNodeReportTransfer)
        {
            this.relayServerNodeReportTransfer = relayServerNodeReportTransfer;
        }
        public async Task<Memory<byte>> Handle(string query)
        {
            return (await relayServerNodeReportTransfer.GetPublicNodes().ConfigureAwait(false)).Select(c =>
            {
                return new
                {
                    AllowProtocol = c.Protocol,
                    Name = c.Name,
                    Version = c.Version,

                    BandwidthMaxMbps = c.Bandwidth,
                    BandwidthConnMbps = c.BandwidthEach,
                    BandwidthCurrentMbps = c.BandwidthRatio,

                    BandwidthGbMonth = c.DataEachMonth,
                    BandwidthByteAvailable = c.DataRemain,

                    ConnectionMaxNum = c.Connections,
                    ConnectionCurrentNum = c.ConnectionsRatio,

                    Url = c.Url,
                    Logo = c.Logo,
                };
            }).ToJson().ToBytes();
        }

        public void Free()
        {
        }

    }
}
