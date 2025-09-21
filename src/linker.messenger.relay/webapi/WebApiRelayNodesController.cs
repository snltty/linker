using linker.libs.extends;
using linker.libs.web;
using linker.messenger.relay.server;

namespace linker.messenger.relay.webapi
{
    public sealed class WebApiRelayNodesController : IWebApiController
    {
        public string Path => "/relay/nodes.json";

        private readonly RelayServerMasterTransfer relayServerMasterTransfer;
        public WebApiRelayNodesController(RelayServerMasterTransfer relayServerMasterTransfer)
        {
            this.relayServerMasterTransfer = relayServerMasterTransfer;
        }
        public Memory<byte> Handle(string query)
        {
            return relayServerMasterTransfer.GetPublicNodes().Select(c =>
            {
                return new
                {
                    AllowProtocol = c.AllowProtocol,
                    Name = c.Name,
                    Version = c.Version,

                    BandwidthMaxMbps = c.MaxBandwidthTotal,
                    BandwidthConnMbps = c.MaxBandwidth,
                    BandwidthCurrentMbps = c.BandwidthRatio,

                    BandwidthGbMonth = c.MaxGbTotal,
                    BandwidthByteAvailable = c.MaxGbTotalLastBytes,

                    ConnectionMaxNum = c.MaxConnection,
                    ConnectionCurrentNum = c.ConnectionRatio,

                    EndPoint = c.EndPoint,
                    Url = c.Url,

                };
            }).ToJson().ToBytes();
        }

        public void Free()
        {
        }

    }
}
