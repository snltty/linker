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
        public string SecretKey { get; set; } = "snltty";
    }

    public sealed class RelayConfigServerInfo
    {
        public string SecretKey { get; set; } = "snltty";
    }

    public sealed class RelayCompactInfo
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public bool Disabled { get; set; }
    }
}
