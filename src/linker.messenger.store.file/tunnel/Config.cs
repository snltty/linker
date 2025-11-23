using linker.messenger.tunnel;
using linker.tunnel.transport;
using LiteDB;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
namespace linker.messenger.store.file
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 打洞配置
        /// </summary>
        public TunnelRunningInfo Tunnel { get; set; } = new TunnelRunningInfo();
    }

    public sealed class TunnelRunningInfo
    {
        public TunnelRunningInfo() { }
        /// <summary>
        /// 附加的网关层级
        /// </summary>
        public int RouteLevelPlus { get; set; }

        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }

        public TunnelPublicNetworkInfo Network { get; set; } = new TunnelPublicNetworkInfo();
        public Dictionary<string, List<TunnelTransportItemInfo>> Transports { get; set; } = new Dictionary<string, List<TunnelTransportItemInfo>>();
    }

    public partial class ConfigClientInfo
    {
        public TunnelConfigClientInfo Tunnel { get; set; } = new TunnelConfigClientInfo();
    }
    public sealed class TunnelConfigClientInfo
    {
        /// <summary>
        /// 打洞协议列表
        /// </summary>
        public List<TunnelTransportItemInfo> Transports { get; set; } = new List<TunnelTransportItemInfo>();
    }
}

