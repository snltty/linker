using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.sforward
{
    public sealed class ConfigSyncSForwardSecretKey : IConfigSync
    {
        public string Name => "SForwardSecretKey";

        private readonly FileConfig fileConfig;
        public ConfigSyncSForwardSecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.SForward.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            fileConfig.Data.Client.SForward.SecretKey = MemoryPackSerializer.Deserialize<string>(data.Span);
            fileConfig.Data.Update();
        }
    }
}
