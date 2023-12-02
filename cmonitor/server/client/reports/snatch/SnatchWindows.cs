using cmonitor.server.client.reports.share;
using common.libs;
using common.libs.extends;

namespace cmonitor.server.client.reports.snatch
{
    public sealed class SnatchWindows : ISnatch
    {
        private readonly Config config;
        private readonly ShareReport shareReport;
        public SnatchWindows(Config config, ShareReport shareReport)
        {
            this.config = config;
            this.shareReport = shareReport;
        }

        public void Set(SnatchQuestionInfo snatchQuestionInfo)
        {
            bool running = shareReport.GetShareRunning(Config.ShareSnatchQuestionIndex);
            //更新问题
            shareReport.UpdateShare(new ShareItemInfo
            {
                Index = Config.ShareSnatchQuestionIndex,
                Key = "SnatchQuestion",
                Value = snatchQuestionInfo.ToJson()
            });
            //未启动，并且未结束
            if (running == false && snatchQuestionInfo.End == false)
            {
                //启动
                shareReport.UpdateShare(new ShareItemInfo
                {
                    Index = Config.ShareSnatchAnswerIndex,
                    Key = "SnatchAnswer",
                    Value = new SnatchAnswerInfo
                    {
                        Result = false,
                        ResultStr = string.Empty,
                        State = SnatchState.Ask,
                        Time = 0,
                    }.ToJson()
                });
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.snatch.win.exe {config.ShareMemoryKey} {config.ShareMemoryLength} {config.ShareMemoryItemSize} {Config.ShareSnatchQuestionIndex} {Config.ShareSnatchAnswerIndex}"
                    });
            }
        }
    }
}
