using cmonitor.client.capi;
using cmonitor.config;
using common.libs.api;
using common.libs.extends;

namespace cmonitor.plugins.relay
{
    public sealed class RelayApiController : IApiClientController
    {
        private readonly Config config;
        private readonly RelayTransfer relayTransfer;

        public RelayApiController(Config config, RelayTransfer relayTransfer)
        {
            this.config = config;
            this.relayTransfer = relayTransfer;
        }

        public List<RelayCompactTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return relayTransfer.GetTypes();
        }

        public bool SetServers(ApiControllerParamsInfo param)
        {
            config.Data.Client.Relay.Servers = param.Content.DeJson<RelayCompactInfo[]>();
            config.Save();
            return true;
        }

    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public string Server { get; set; }
    }
}
