using linker.config;

namespace linker.plugins.updater
{
    public sealed class UpdaterServerTransfer
    {

        public string SecretKey => fileConfig.Data.Server.Updater.SecretKey;

        private UpdateInfo updateInfo = new UpdateInfo { Status = UpdateStatus.Checked };
        private readonly UpdaterHelper updaterHelper;
        private readonly FileConfig fileConfig;

        public UpdaterServerTransfer(UpdaterHelper updaterHelper, FileConfig fileConfig)
        {
            this.updaterHelper = updaterHelper;
            this.fileConfig = fileConfig;
        }

        public UpdateInfo Get()
        {
            return updateInfo;
        }
        /// <summary>
        /// 确认更新
        /// </summary>
        public void Confirm(string version)
        {
            updaterHelper.Confirm(updateInfo, version);
        }

    }


}
