using linker.messenger.sforward.server;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerStore : ISForwardServerStore
    {
        public string SecretKey => fileConfig.Data.Server.SForward.SecretKey;

        public byte BufferSize => fileConfig.Data.Server.SForward.BufferSize;

        public int WebPort => fileConfig.Data.Server.SForward.WebPort;

        public int[] TunnelPortRange => fileConfig.Data.Server.SForward.TunnelPortRange;

        private readonly FileConfig fileConfig;
        public SForwardServerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
    }
}
