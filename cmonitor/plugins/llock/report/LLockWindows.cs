using cmonitor.config;
using common.libs;
using common.libs.winapis;

namespace cmonitor.plugins.llock.report
{
    public sealed class LLockWindows : ILLock
    {
        private readonly Config config;
        public LLockWindows(Config config)
        {
            this.config = config;
        }

        public void Set(bool value)
        {
            if (value)
            {
                CommandHelper.Windows(string.Empty, new string[] {
                        $"start cmonitor.llock.win.exe {config.Client.ShareMemoryKey} {config.Client.ShareMemoryCount} {config.Client.ShareMemorySize} {(int)ShareMemoryIndexs.LLock}"
                    },false);
            }
        }

        public void LockSystem()
        {
            User32.LockWorkStation();
        }
    }
}
