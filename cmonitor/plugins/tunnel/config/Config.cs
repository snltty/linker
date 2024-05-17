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

        [JsonIgnore]
        public int RouteLevel { get; set; }

        [JsonIgnore]
        public IPAddress[] LocalIPs { get; set; }
    }

    public sealed class TunnelCompactInfo
    {
        public string Name { get; set; }
        public TunnelCompactType Type { get; set; }
        public string Host { get; set; }
        public bool Disabled { get; set; }
    }

    public enum TunnelCompactType : byte
    {
        Self = 0
    }
}
