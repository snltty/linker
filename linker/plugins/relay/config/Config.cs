using linker.client.config;
using linker.config;
using LiteDB;
using MemoryPack;
using System.Diagnostics.CodeAnalysis;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 中继配置
        /// </summary>
        public RelayRunningInfo Relay { get; set; } = new RelayRunningInfo();
    }

    public sealed class RelayRunningInfo
    {
        public ObjectId Id { get; set; }
        /// <summary>
        /// 中继服务器列表
        /// </summary>
        public RelayServerInfo[] Servers { get; set; } = Array.Empty<RelayServerInfo>();

        public bool ByRelay { get; set; }
    }
}


namespace linker.config
{

    public sealed partial class ConfigClientInfo
    {
        public RelayInfo Relay { get; set; } = new RelayInfo();
    }
    public sealed class RelayInfo
    {
        /// <summary>
        /// 中继服务器列表
        /// </summary>
        public RelayServerInfo[] Servers { get; set; } = Array.Empty<RelayServerInfo>();

    }


    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 中继配置
        /// </summary>
        public RelayConfigServerInfo Relay { get; set; } = new RelayConfigServerInfo();
    }
    public sealed class RelayConfigServerInfo
    {
        /// <summary>
        /// 中继密钥
        /// </summary>
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;
    }

    /// <summary>
    /// 中继服务器
    /// </summary>
    [MemoryPackable]
    public sealed partial class RelayServerInfo
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 中继服务器类别
        /// </summary>
        public RelayType RelayType { get; set; } = RelayType.Linker;
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = "snltty";
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; set; } = string.Empty;
        /// <summary>
        /// 禁用
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// 开启ssl
        /// </summary>
        public bool SSL { get; set; } = true;

        /// <summary>
        /// 延迟
        /// </summary>
        public int Delay { get; set; }
        /// <summary>
        /// 可用
        /// </summary>
        public bool Available { get; set; }
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
