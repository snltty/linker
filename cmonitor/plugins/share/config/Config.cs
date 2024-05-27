namespace cmonitor.config
{
    public partial class ConfigClientInfo
    {
        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryCount { get; set; } = 100;
        public int ShareMemorySize { get; set; } = 1024;
    }

}
