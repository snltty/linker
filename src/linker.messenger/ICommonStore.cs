namespace linker.messenger
{
    public interface ICommonStore
    {
        public CommonModes Modes { get; }
        public bool Installed { get; }

        public void SetModes(CommonModes modes);
        public void SetInstalled(bool installed);
        public void Confirm();
    }

    [Flags]
    public enum CommonModes : byte
    {
        Client = 1,
        Server = 2,
        All = 1 | 2
    }
}
