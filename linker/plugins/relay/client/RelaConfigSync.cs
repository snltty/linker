using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.relay.client
{
    public sealed class RelaConfigSyncSecretKey : IConfigSync
    {
        public string Name => "RelaySecretKey";

        private readonly RelayClientConfigTransfer relayClientConfigTransfer;
        public RelaConfigSyncSecretKey(RelayClientConfigTransfer relayClientConfigTransfer)
        {
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
