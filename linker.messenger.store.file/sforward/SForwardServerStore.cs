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

        public bool SetSecretKey(string key)
        {
            fileConfig.Data.Server.SForward.SecretKey = key;
            return true;
        }

        public bool SetBufferSize(byte size)
        {
            fileConfig.Data.Server.SForward.BufferSize = size;
            return true;
        }

        public bool SetWebPort(int port)
        {
            fileConfig.Data.Server.SForward.WebPort = port;
            return true;
        }

        public bool SetTunnelPortRange(int[] ports)
        {
            if (ports == null || ports.Length != 2) return false;
            fileConfig.Data.Server.SForward.TunnelPortRange = ports;
            return true;
        }

        public bool Confirm()
        {
            fileConfig.Data.Update();
            return true;
        }
    }
}
