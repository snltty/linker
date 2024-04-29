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
        public TunnelCompactInfo[] Servers { get; set; } = new TunnelCompactInfo[]
        {
            new TunnelCompactInfo { Type="self", Host="127.0.0.1:1804" }
        };

        [JsonIgnore]
        public int RouteLevel { get; set; }
    }

    public sealed class TunnelConfigServerInfo
    {
        public int ListenPort { get; set; } = 1804;
    }

    public sealed class TunnelCompactInfo
    {
        public string Type { get; set; }
        public string Host { get; set; }
        public bool Disabled { get; set; }
    }
}
