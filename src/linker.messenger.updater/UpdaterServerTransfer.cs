using linker.libs;
using linker.libs.timer;

namespace linker.messenger.updater
{
    public sealed class UpdaterServerTransfer
    {
        private UpdaterInfo updateInfo = new UpdaterInfo { Status = UpdaterStatus.Checked };
        private readonly UpdaterHelper updaterHelper;
        private readonly IUpdaterCommonStore updaterCommonTransfer;
        public UpdaterServerTransfer(UpdaterHelper updaterHelper, IUpdaterCommonStore updaterCommonTransfer)
        {
            this.updaterHelper = updaterHelper;
            this.updaterCommonTransfer = updaterCommonTransfer;
            CheckTask();

            Helper.OnUpdate += (sender, version) =>
            {
                updaterHelper.Confirm(updateInfo, version);
            };
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

        private void CheckTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if (updaterCommonTransfer.CheckUpdate)
                {
                    await updaterHelper.GetUpdateInfo(updateInfo).ConfigureAwait(false);
                }
            }, () => updaterCommonTransfer.UpdateIntervalSeconds * 1000);
        }
    }
}
