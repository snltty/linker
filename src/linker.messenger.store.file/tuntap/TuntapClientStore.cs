using linker.messenger.tuntap;

namespace linker.messenger.store.file.tuntap
{
    public sealed class TuntapClientStore : ITuntapClientStore
    {
        public TuntapConfigInfo Info => runningConfig.Data.Tuntap;

        private readonly RunningConfig runningConfig;
        public TuntapClientStore(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }
        public void Confirm()
        {
            runningConfig.Data.Update();
        }
    }
}
