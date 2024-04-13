namespace cmonitor.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();
    }
    public sealed partial class ConfigServerInfo
    {
        public int WebPort { get; set; } = 1800;
        public string WebRoot { get; set; } = "./web/";
        public int ApiPort { get; set; } = 1801;
        public int ServicePort { get; set; } = 1802;
    }
}
