using linker.config;

namespace linker.plugins.sforward
{
    public sealed class SForwardServerConfigTransfer
    {
        public string SecretKey => config.Data.Server.SForward.SecretKey;
        public byte BufferSize => config.Data.Server.SForward.BufferSize;
        public int WebPort => config.Data.Server.SForward.WebPort;
        public int[] TunnelPortRange => config.Data.Server.SForward.TunnelPortRange;

        private readonly FileConfig config;
        public SForwardServerConfigTransfer(FileConfig config)
        {
            this.config = config;
        }
    }
}
