using linker.messenger.updater;

namespace linker.messenger.store.file.updater
{
    public sealed class UpdaterClientStore : IUpdaterClientStore
    {
        public string SecretKey => fileConfig.Data.Client.Updater.SecretKey;

        private readonly FileConfig fileConfig;
        public UpdaterClientStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public void SetSecretKey(string key)
        {
            fileConfig.Data.Client.Updater.SecretKey = key;
            fileConfig.Data.Update();
        }
    }
}
