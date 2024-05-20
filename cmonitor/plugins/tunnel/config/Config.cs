using System.Diagnostics.CodeAnalysis;
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
        public string Name { get; set; } = string.Empty;
        public TunnelCompactType Type { get; set; }
        public string Host { get; set; } = string.Empty;
        public bool Disabled { get; set; }
    }

    public enum TunnelCompactType : byte
    {
        Self = 0
    }

    public sealed class TunnelCompactTypeInfo
    {
        public TunnelCompactType Value { get; set; }
        public string Name { get; set; }
    }

    public sealed class TunnelCompactTypeInfoEqualityComparer : IEqualityComparer<TunnelCompactTypeInfo>
    {
        public bool Equals(TunnelCompactTypeInfo x, TunnelCompactTypeInfo y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] TunnelCompactTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
