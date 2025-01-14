namespace linker.messenger.updater
{
    public sealed class UpdaterServerTransfer
    {
        private UpdaterInfo updateInfo = new UpdaterInfo { Status = UpdaterStatus.Checked };
        private readonly UpdaterHelper updaterHelper;

        public UpdaterServerTransfer(UpdaterHelper updaterHelper)
        {
            this.updaterHelper = updaterHelper;
        }

        public UpdaterInfo Get()
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
