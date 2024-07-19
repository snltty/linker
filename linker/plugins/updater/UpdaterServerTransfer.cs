namespace linker.plugins.updater
{
    public sealed class UpdaterServerTransfer
    {
        private UpdateInfo updateInfo = new UpdateInfo { Status = UpdateStatus.Checked };
        private readonly UpdaterHelper updaterHelper;
        public UpdaterServerTransfer(UpdaterHelper updaterHelper)
        {
            this.updaterHelper = updaterHelper;
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
