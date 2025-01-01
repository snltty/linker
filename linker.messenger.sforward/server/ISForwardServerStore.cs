namespace linker.messenger.sforward.server
{
    public interface ISForwardServerStore
    {
        public string SecretKey { get; }
        public byte BufferSize { get; }
        public int WebPort { get; }
        public int[] TunnelPortRange { get; }
    }
}
