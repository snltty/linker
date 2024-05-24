using MemoryPack;
using System.Net;

namespace cmonitor.plugins.tunnel.compact
{
    public interface ITunnelCompact
    {
        public string Name { get; }
        public TunnelCompactType Type { get; }
        public Task<TunnelCompactIPEndPoint> GetExternalIPAsync(IPEndPoint server);
    }

    public sealed class TunnelCompactIPEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }
    }

    [MemoryPackable]
    public sealed partial class TunnelCompactExternalIPInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint ExternalIP { get; set; }
    }



    [MemoryPackable]
    public sealed partial class TunnelCompactInfo
    {
        public string Name { get; set; } = string.Empty;
        public TunnelCompactType Type { get; set; }
        public string Host { get; set; } = string.Empty;
        public bool Disabled { get; set; }
    }

    public enum TunnelCompactType : byte
    {
        Self = 0,
        Stun = 1
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

        public int GetHashCode(TunnelCompactTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }


}
