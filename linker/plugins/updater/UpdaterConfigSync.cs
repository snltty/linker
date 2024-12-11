using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.updater
{
    public sealed class UpdaterConfigSyncSecretKey : IConfigSync
    {
        public string Name => "UpdaterSecretKey";

        private readonly UpdaterClientTransfer updaterClientTransfer;
        public UpdaterConfigSyncSecretKey(UpdaterClientTransfer updaterClientTransfer)
        {
            this.updaterClientTransfer = updaterClientTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(updaterClientTransfer.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            updaterClientTransfer.SetSecretKey(MemoryPackSerializer.Deserialize<string>(data.Span));
        }
    }
}
