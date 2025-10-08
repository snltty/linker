namespace linker.messenger.store.file
{
    public sealed partial class ConfigServerInfo
    {
        public int ServicePort { get; set; } = 1802;
        public int ApiPort { get; set; } = 1803;

        public string[] Hosts { get; set; } = [];
    }
}
