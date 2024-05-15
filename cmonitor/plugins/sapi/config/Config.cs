namespace cmonitor.config
{
    public partial class ConfigServerInfo
    {
        public SApiConfigServerInfo SApi { get; set; } = new SApiConfigServerInfo();
    }

    public sealed class SApiConfigServerInfo
    {
        public int WebPort { get; set; } = 1800;
        public string WebRoot { get; set; } = "./web/";
        public int ApiPort { get; set; } = 1801;
        public string ApiPassword { get; set; } = "snltty";
    }
}
