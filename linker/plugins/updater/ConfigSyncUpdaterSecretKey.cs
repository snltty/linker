using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.updater
{
    public sealed class ConfigSyncUpdaterSecretKey : IConfigSync
    {
        public string Name => "UpdaterSecretKey";

        private readonly FileConfig fileConfig;
        public ConfigSyncUpdaterSecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.Updater.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            fileConfig.Data.Client.Updater.SecretKey = MemoryPackSerializer.Deserialize<string>(data.Span);
            fileConfig.Data.Update();
        }
    }
}
