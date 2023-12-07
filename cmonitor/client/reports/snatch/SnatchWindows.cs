using common.libs;

namespace cmonitor.client.reports.snatch
{
    public sealed class SnatchWindows : ISnatch
    {
        private readonly Config config;

        public SnatchWindows(Config config)
        {
            this.config = config;
        }

        public void StartUp(SnatchQuestionInfo snatchQuestionInfo)
        {
            CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.snatch.win.exe {config.ShareMemoryKey} {config.ShareMemoryLength} {config.ShareMemoryItemSize} {Config.ShareSnatchQuestionIndex} {Config.ShareSnatchAnswerIndex}"
                    });
        }
    }
}
