using linker.libs;
using linker.plugins.relay.transport;
using MemoryPack;

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
        public RelayServerInfo[] Servers { get; set; } = new RelayServerInfo[] { new RelayServerInfo { Delay = -1 } };
        public RelayServerInfo Server => Servers[0];

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
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif

        public RelayCacheInfo Caching { get; set; } = new RelayCacheInfo { };
    }
    public sealed class RelayCacheInfo
    {
        public string Name { get; set; } = "memory";
        public string ConnectString { get; set; } = string.Empty;
    }

    /// <summary>
    /// 中继服务器
    /// </summary>
    [MemoryPackable]
    public sealed partial class RelayServerInfo
    {
        public RelayServerInfo() { }
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = Helper.GlobalString;
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

        public RelayType RelayType { get; set; } = RelayType.Linker;
    }

}
