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

        public string HostName { get; set; }
        public TunnelInterfaceInfo[] Lans { get; set; } = Array.Empty<TunnelInterfaceInfo>();
        public IPAddress[] Routes { get; set; } = Array.Empty<IPAddress>();


        public TunnelNetInfo Net { get; set; } = new TunnelNetInfo();
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
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Isp { get; set; } = string.Empty;
        public string Org { get; set; } = string.Empty;
        public string As { get; set; } = string.Empty;
    }

    public sealed partial class TunnelSetRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevelPlus { get; set; }
        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
    }
}
