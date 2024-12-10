using linker.config;
using linker.plugins.config;
using MemoryPack;

namespace linker.plugins.client
{
    public sealed class ConfigSyncSignInSecretKey : IConfigSync
    {
        public string Name => "SignInSecretKey";

        private readonly FileConfig fileConfig;
        private readonly ClientConfigTransfer clientConfigTransfer;
        public ConfigSyncSignInSecretKey(FileConfig fileConfig, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.clientConfigTransfer = clientConfigTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(clientConfigTransfer.Server.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            clientConfigTransfer.Server.SecretKey = MemoryPackSerializer.Deserialize<string>(data.Span);
            fileConfig.Data.Update();
        }
    }
    public sealed class ConfigSyncGroupSecretKey : IConfigSync
    {
        public string Name => "GroupSecretKey";

        private readonly FileConfig fileConfig;
        private readonly ClientConfigTransfer clientConfigTransfer;
        public ConfigSyncGroupSecretKey(FileConfig fileConfig, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.clientConfigTransfer = clientConfigTransfer;
        }
        public Memory<byte> GetData()
        {
            return MemoryPackSerializer.Serialize(clientConfigTransfer.Group.Password);
        }

        public void SetData(Memory<byte> data)
        {
            clientConfigTransfer.SetGroupPassword(MemoryPackSerializer.Deserialize<string>(data.Span));
        }
    }
}
