namespace linker.messenger.store.file
{

    public sealed partial class ConfigCommonInfo
    {
        public string[] Modes { get; set; } = new string[] { "client", "server" };
#if DEBUG
        public bool Install { get; set; } = false;
#else
        public bool Install { get; set; } = false;
#endif
    }

}
