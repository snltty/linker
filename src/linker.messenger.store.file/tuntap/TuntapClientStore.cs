using linker.messenger.tuntap;
using linker.messenger.tuntap.client;

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
