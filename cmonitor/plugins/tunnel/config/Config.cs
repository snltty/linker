using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.transport;
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

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "snltty";

        public List<TunnelTransportItemInfo> TunnelTransports { get; set; } = new List<TunnelTransportItemInfo>();

        [JsonIgnore]
        public int RouteLevel { get; set; }

        [JsonIgnore]
        public IPAddress[] LocalIPs { get; set; }
    }



}
