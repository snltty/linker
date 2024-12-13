using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.sforward
{
    public sealed class SForwardConfigSyncSecretKey : IConfigSync
    {
        public string Name => "SForwardSecretKey";

        private readonly SForwardTransfer sForwardTransfer;
        public SForwardConfigSyncSecretKey(SForwardTransfer sForwardTransfer)
        {
            this.sForwardTransfer = sForwardTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(sForwardTransfer.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            sForwardTransfer.SetSecretKey(MemoryPackSerializer.Deserialize<string>(data.Span));
        }
    }
}
