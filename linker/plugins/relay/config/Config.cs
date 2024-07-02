using linker.config;
using LiteDB;
using MemoryPack;
using System.Diagnostics.CodeAnalysis;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public RelayRunningInfo Relay { get; set; } = new RelayRunningInfo();
    }

    public sealed class RelayRunningInfo
    {
        public ObjectId Id { get; set; }
        public RelayServerInfo[] Servers { get; set; } = Array.Empty<RelayServerInfo>();
    }
}


namespace linker.config
{
    public partial class ConfigServerInfo
    {
        public RelayConfigServerInfo Relay { get; set; } = new RelayConfigServerInfo();
    }

    public sealed class RelayConfigServerInfo
    {
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public byte BufferSize { get; set; } = 3;
    }

    [MemoryPackable]
    public sealed partial class RelayServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public RelayType RelayType { get; set; } = RelayType.Linker;
        public string SecretKey { get; set; } = "snltty";
        public string Host { get; set; } = string.Empty;
        public bool Disabled { get; set; }
        public bool SSL { get; set; } = true;
    }

    public enum RelayType : byte
    {
        Linker = 0,
    }

    public sealed class RelayTypeInfo
    {
        public RelayType Value { get; set; }
        public string Name { get; set; }
    }

    public sealed class RelayCompactTypeInfoEqualityComparer : IEqualityComparer<RelayTypeInfo>
    {
        public bool Equals(RelayTypeInfo x, RelayTypeInfo y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] RelayTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}
