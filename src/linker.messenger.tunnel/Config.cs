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
    }

    public sealed partial class TunnelSetRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevelPlus { get; set; }
        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
    }
}
