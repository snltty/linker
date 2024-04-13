using cmonitor.config;
using common.libs;

namespace cmonitor.plugins.snatch.report
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
                        $"start cmonitor.snatch.win.exe {config.Data.Client.ShareMemoryKey} {config.Data.Client.ShareMemoryCount} {config.Data.Client.ShareMemorySize} {(int)ShareMemoryIndexs.SnatchQuestion} {(int)ShareMemoryIndexs.SnatchAnswer}"
                    },false);
        }
    }
}
