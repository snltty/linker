using linker.messenger.updater;

namespace linker.messenger.store.file.updater
{
    public sealed class UpdaterClientStore : IUpdaterClientStore
    {
        public UpdaterConfigClientInfo Info => fileConfig.Data.Client.Updater;

        private readonly FileConfig fileConfig;
        public UpdaterClientStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
        public void SetSync2Server(bool value)
        {
            fileConfig.Data.Client.Updater.Sync2Server = value;
            fileConfig.Data.Update();
        }
    }
}
