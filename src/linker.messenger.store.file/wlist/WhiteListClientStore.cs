using linker.messenger.wlist;

namespace linker.messenger.store.file.wlist
{
    public sealed class WhiteListClientStore : IWhiteListClientStore
    {
        public string SecretKey => fileConfig.Data.Client.WhiteList.SecretKey;

        private readonly FileConfig fileConfig;
        public WhiteListClientStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public bool SetSecretKey(string secretKey)
        {
            fileConfig.Data.Client.WhiteList.SecretKey = secretKey;
            fileConfig.Data.Update();
            return true;
        }
    }
}
