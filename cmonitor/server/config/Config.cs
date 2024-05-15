namespace cmonitor.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();
    }
    public sealed partial class ConfigServerInfo
    {
        public int ServicePort { get; set; } = 1802;
    }
}
