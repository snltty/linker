namespace linker.messenger.store.file
{

    public sealed partial class ConfigCommonInfo
    {
        public string[] Modes { get; set; } = new string[] { "client", "server" };
    }

}
