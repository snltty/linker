namespace linker.messenger.rapp
{
    public sealed class RemoteAppInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string IconPath { get; set; }
        public byte IconIndex { get; set; }
        public string VPath { get; set; }
        public byte ShowInTSWA { get; set; } = 1;
        public byte CommandLineSetting { get; set; } = 1;
    }
}
