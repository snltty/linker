using System.Net;

namespace linker.messenger.tunnel
{
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
    }

    public sealed partial class TunnelInterfaceInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Mac { get; set; }
        public IPAddress[] Ips { get; set; } = Array.Empty<IPAddress>();
    }

    public sealed partial class TunnelSetRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevelPlus { get; set; }
        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
    }
}
