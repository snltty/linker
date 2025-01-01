namespace linker.messenger
{
    public interface ICommonStore
    {
        public CommonModes Modes { get; }
    }

    [Flags]
    public enum CommonModes : byte
    {
        Client = 1,
        Server = 2
    }
}
