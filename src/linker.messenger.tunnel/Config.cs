using linker.tunnel.transport;
using System.Net;

namespace linker.messenger.tunnel
{

    /// <summary>
    /// Chinanet 电信
    /// China Unicom 连通
    /// China Mobile 移动
    /// </summary>
    public sealed partial class TunnelRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevel { get; set; }
        public int RouteLevelPlus { get; set; }

        public bool NeedReboot { get; set; }

        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
        public TunnelNetInfo Net { get; set; } = new TunnelNetInfo();
    }

    public sealed partial class TunnelLocalNetworkInfo
    {
        public string MachineId { get; set; }
        public string HostName { get; set; }
        public TunnelInterfaceInfo[] Lans { get; set; } = Array.Empty<TunnelInterfaceInfo>();
        public IPAddress[] Routes { get; set; } = Array.Empty<IPAddress>();
    }

    public sealed partial class TunnelInterfaceInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Mac { get; set; }
        public IPAddress[] Ips { get; set; } = Array.Empty<IPAddress>();
    }

    public sealed partial class TunnelNetInfo
    {
        public string City { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Isp { get; set; } = string.Empty;
        public string Nat { get; set; } = string.Empty;
    }

    public sealed partial class TunnelSetRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevelPlus { get; set; }
        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
    }

    public sealed class TunnelPublicNetworkInfo
    {
        /// <summary>
        /// 网关层级
        /// </summary>
        public int RouteLevel { get; set; }
        /// <summary>
        /// 本地IP
        /// </summary>
        public IPAddress[] LocalIPs { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 路由上的IP
        /// </summary>
        public IPAddress[] RouteIPs { get; set; } = Array.Empty<IPAddress>();

        public TunnelNetInfo Net { get; set; } = new TunnelNetInfo();
    }

    public sealed class TunnelTransportItemSetInfo
    {
        /// <summary>
        /// 发送方填对方ID，服务端会转换，接收方收到的也是对方ID
        /// </summary>
        public string MachineId { get; set; }
        public List<TunnelTransportItemInfo> Data { get; set; }
    }

}
