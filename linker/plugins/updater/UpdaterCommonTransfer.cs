using linker.config;

namespace linker.plugins.updater
{
    public sealed class UpdaterCommonTransfer
    {

        public string UpdateUrl => fileConfig.Data.Common.UpdateUrl;
        public int UpdateIntervalSeconds => fileConfig.Data.Common.UpdateIntervalSeconds;

        private readonly FileConfig fileConfig;

        public UpdaterCommonTransfer( FileConfig fileConfig)
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
