using System.Text.Json.Serialization;

namespace cmonitor.config
{
    public partial class ConfigClientInfo
    {
        public TunnelConfigClientInfo Tunnel { get; set; } = new TunnelConfigClientInfo();
    }
    public partial class ConfigServerInfo
    {
        public TunnelConfigServerInfo Tunnel { get; set; } = new TunnelConfigServerInfo();
    }

    public sealed class TunnelConfigClientInfo
    {
        public TunnelCompactInfo[] Servers { get; set; } = Array.Empty<TunnelCompactInfo>();

        [JsonIgnore]
        public int RouteLevel { get; set; }
    }

    public sealed class TunnelConfigServerInfo
    {
    }

    public sealed class TunnelCompactInfo
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public bool Disabled { get; set; }
    }
}
