using cmonitor.client.capi;
using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using common.libs.api;
using common.libs.extends;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly Config config;
        private readonly CompactTransfer compactTransfer;

        public TunnelApiController(Config config, CompactTransfer compactTransfer)
        {
            this.config = config;
            this.compactTransfer = compactTransfer;
        }

        public List<TunnelCompactTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return compactTransfer.GetTypes();
        }

        public bool SetServers(ApiControllerParamsInfo param)
        {
            config.Data.Client.Tunnel.Servers = param.Content.DeJson<TunnelCompactInfo[]>();
            config.Save();
            return true;
        }
    }

}
