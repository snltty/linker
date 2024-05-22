using cmonitor.plugins.tunnel.compact;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.config
{
    public partial class ConfigClientInfo
    {
        public TunnelConfigClientInfo Tunnel { get; set; } = new TunnelConfigClientInfo();
    }
    public sealed class TunnelConfigClientInfo
    {
        public TunnelCompactInfo[] Servers { get; set; } = Array.Empty<TunnelCompactInfo>();
        public int RouteLevelPlus { get; set; } = 0;

        [JsonIgnore]
        public int RouteLevel { get; set; }

        [JsonIgnore]
        public IPAddress[] LocalIPs { get; set; }
    }

    

}
