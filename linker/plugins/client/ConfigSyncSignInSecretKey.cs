using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.client
{
    public sealed class ConfigSyncSignInSecretKey : IConfigSync
    {
        public string Name => "SignInSecretKey";

        private readonly FileConfig fileConfig;
        public ConfigSyncSignInSecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.Servers.FirstOrDefault().SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            string value = MemoryPackSerializer.Deserialize<string>(data.Span);
            foreach (var item in fileConfig.Data.Client.Servers)
            {
                item.SecretKey = value;
            }
            fileConfig.Data.Update();
        }
    }
    public sealed class ConfigSyncGroupSecretKey : IConfigSync
    {
        public string Name => "GroupSecretKey";

        private readonly FileConfig fileConfig;
        public ConfigSyncGroupSecretKey(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(fileConfig.Data.Client.Group.Password);
        }

        public void SetData(Memory<byte> data)
        {
            fileConfig.Data.Client.Group.Password = MemoryPackSerializer.Deserialize<string>(data.Span);
            fileConfig.Data.Update();
        }
    }
}
