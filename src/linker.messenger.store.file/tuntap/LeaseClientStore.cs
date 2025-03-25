using linker.messenger.tuntap;
using linker.messenger.tuntap.lease;

namespace linker.messenger.store.file.tuntap
{
    public sealed class LeaseClientStore : ILeaseClientStore
    {
        public TuntapConfigInfo Info => runningConfig.Data.Tuntap;

        private readonly RunningConfig runningConfig;
        public LeaseClientStore(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }

        public LeaseInfo Get(string key)
        {
            if (runningConfig.Data.Leases.TryGetValue(key, out LeaseInfo info))
            {
                return info;
            }
            return new LeaseInfo();
        }

        public bool Set(string key, LeaseInfo info)
        {
            runningConfig.Data.Leases.AddOrUpdate(key, info, (a, b) => info);
            return true;
        }
        public void Confirm()
        {
            runningConfig.Data.Update();
        }


    }
}
