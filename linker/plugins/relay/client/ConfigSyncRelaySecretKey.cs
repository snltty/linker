using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.relay.client
{
    public sealed class ConfigSyncRelaySecretKey : IConfigSync
    {
        public string Name => "RelaySecretKey";

        private readonly FileConfig fileConfig;
        public ConfigSyncRelaySecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.Relay.Servers.FirstOrDefault().SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            string value = MemoryPackSerializer.Deserialize<string>(data.Span);
            foreach (var item in fileConfig.Data.Client.Relay.Servers)
            {
                item.SecretKey = value;
            }
            fileConfig.Data.Update();
        }
    }
}
