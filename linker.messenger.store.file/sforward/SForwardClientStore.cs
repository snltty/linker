using linker.messenger.sforward;
using linker.messenger.sforward.client;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardClientStore : ISForwardClientStore
    {
        public string SecretKey => fileConfig.Data.Client.SForward.SecretKey;

        private readonly FileConfig fileConfig;
        private readonly RunningConfig runningConfig;
        public SForwardClientStore(FileConfig fileConfig, RunningConfig runningConfig)
        {
            this.fileConfig = fileConfig;
            this.runningConfig = runningConfig;
            foreach (var item in runningConfig.Data.SForwards)
            {
                item.Proxy = false;
            }
        }
        public bool SetSecretKey(string key)
        {
            fileConfig.Data.Client.SForward.SecretKey = key;
            fileConfig.Data.Update();
            return true;
        }


        public int Count()
        {
            return runningConfig.Data.SForwards.Count();
        }

        public List<SForwardInfo> Get()
        {
            return runningConfig.Data.SForwards;
        }

        public SForwardInfo Get(uint id)
        {
            return runningConfig.Data.SForwards.FirstOrDefault(x => x.Id == id);
        }

        public SForwardInfo Get(string domain)
        {
            return runningConfig.Data.SForwards.FirstOrDefault(x => x.Domain == domain);
        }

        public SForwardInfo Get(int port)
        {
            return runningConfig.Data.SForwards.FirstOrDefault(c => c.RemotePort == port);
        }

        public bool Add(SForwardInfo info)
        {
            runningConfig.Data.SForwards.Add(info);
            return true;
        }
        public bool Update(SForwardInfo info)
        {
            return true;
        }
        public bool Remove(uint id)
        {
            runningConfig.Data.SForwards.Remove(Get(id));
            return true;
        }

        public bool Confirm()
        {
            runningConfig.Data.Update();
            return true;
        }


    }
}
