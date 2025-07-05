
using linker.messenger.updater;

namespace linker.messenger.store.file.updater
{
    public sealed class UpdaterServerStore : IUpdaterServerStore
    {
        public bool Sync2Server => fileConfig.Data.Server.Updater.Sync2Server;

        private readonly FileConfig fileConfig;
        public UpdaterServerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

      

        public bool Confirm()
        {
            fileConfig.Data.Update();
            return true;
        }
    }
}
