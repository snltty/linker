
using linker.messenger.updater;
namespace linker.messenger.store.file.updater
{
    public sealed class UpdaterCommonStore : IUpdaterCommonStore
    {
        public string UpdateUrl => fileConfig.Data.Common.UpdateUrl;

        public int UpdateIntervalSeconds => fileConfig.Data.Common.UpdateIntervalSeconds;

        public bool CheckUpdate => fileConfig.Data.Common.CheckUpdate;

        private readonly FileConfig fileConfig;
        public UpdaterCommonStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public void SetInterval(int sec)
        {
            fileConfig.Data.Common.UpdateIntervalSeconds = sec;
            fileConfig.Data.Update();
        }
    }
}
