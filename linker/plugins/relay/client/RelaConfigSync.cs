using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.relay.client
{
    public sealed class RelaConfigSyncSecretKey : IConfigSync
    {
        public string Name => "RelaySecretKey";

        private readonly FileConfig fileConfig;
        private readonly RelayClientConfigTransfer relayClientConfigTransfer;
        public RelaConfigSyncSecretKey(FileConfig fileConfig, RelayClientConfigTransfer relayClientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.relayClientConfigTransfer = relayClientConfigTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(relayClientConfigTransfer.Server.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            string value = MemoryPackSerializer.Deserialize<string>(data.Span);
            relayClientConfigTransfer.SetServerSecretKey(value);
        }
    }
}
