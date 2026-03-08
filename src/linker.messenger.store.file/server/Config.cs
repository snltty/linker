using linker.messenger.listen;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigServerInfo
    {
        public int ServicePort { get; set; } = 1802;
        public int ApiPort { get; set; } = 1803;

        public bool Ipv6 { get; set; } = false;

        public string[] Hosts { get; set; } = [];

        public GeoRegistryInfo GeoRegistry { get; set; } = new GeoRegistryInfo();
    }
}
