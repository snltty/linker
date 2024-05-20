using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace cmonitor.config
{
    public partial class ConfigClientInfo
    {
        public RelayConfigClientInfo Relay { get; set; } = new RelayConfigClientInfo();
    }
    public partial class ConfigServerInfo
    {
        public RelayConfigServerInfo Relay { get; set; } = new RelayConfigServerInfo();
    }

    public sealed class RelayConfigClientInfo
    {
        public RelayCompactInfo[] Servers { get; set; } = Array.Empty<RelayCompactInfo>();
    }

    public sealed class RelayConfigServerInfo
    {
        public string SecretKey { get; set; } = "snltty";
    }

    public sealed class RelayCompactInfo
    {
        public string Name { get; set; } = string.Empty;
        public RelayCompactType Type { get; set; } = RelayCompactType.Self;
        public string SecretKey { get; set; } = "snltty";
        public string Host { get; set; } = string.Empty;
        public bool Disabled { get; set; }
    }

    public enum RelayCompactType : byte
    {
        Self = 0,
    }

    public sealed class RelayCompactTypeInfo
    {
        public RelayCompactType Value { get; set; }
        public string Name { get; set; }
    }

    public sealed class RelayCompactTypeInfoEqualityComparer : IEqualityComparer<RelayCompactTypeInfo>
    {
        public bool Equals(RelayCompactTypeInfo x, RelayCompactTypeInfo y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] RelayCompactTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
