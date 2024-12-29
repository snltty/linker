
using linker.messenger.updater;

namespace linker.messenger.store.file.updater
{
    public sealed class UpdaterServerStore : IUpdaterServerStore
    {
        public string SecretKey => fileConfig.Data.Server.Updater.SecretKey;

        private readonly FileConfig fileConfig;
        public UpdaterServerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
    }
}
