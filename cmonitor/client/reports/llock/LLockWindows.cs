using common.libs;
using common.libs.winapis;

namespace cmonitor.client.reports.llock
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
                        $"start llock.win.exe {config.ShareMemoryKey} {config.ShareMemoryLength} {config.ShareMemoryItemSize} {Config.ShareMemoryLLockIndex}"
                    });
            }
        }

        public void LockSystem()
        {
            User32.LockWorkStation();
        }
    }
}
