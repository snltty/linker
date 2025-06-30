using linker.messenger.cdkey;

namespace linker.messenger.store.file.cdkey
{
    public sealed class CdkeyClientStore : ICdkeyClientStore
    {
        public string SecretKey => fileConfig.Data.Client.Cdkey.SecretKey;

        private readonly FileConfig fileConfig;
        public CdkeyClientStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public bool SetSecretKey(string secretKey)
        {
            fileConfig.Data.Client.Cdkey.SecretKey = secretKey;
            fileConfig.Data.Update();
            return true;
        }
    }
}
