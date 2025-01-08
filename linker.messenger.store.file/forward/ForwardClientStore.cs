using linker.messenger.forward;

namespace linker.messenger.store.file.forward
{
    public sealed class ForwardClientStore : IForwardClientStore
    {
        private readonly RunningConfig runningConfig;
        public ForwardClientStore(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
            foreach (var item in runningConfig.Data.Forwards)
            {
                item.Proxy = false;
            }
        }
        public int Count()
        {
            return runningConfig.Data.Forwards.Count();
        }

        public List<ForwardInfo> Get()
        {
            return runningConfig.Data.Forwards;
        }

        public ForwardInfo Get(uint id)
        {
            return runningConfig.Data.Forwards.FirstOrDefault(x => x.Id == id);
        }

        public List<ForwardInfo> Get(string groupid)
        {
            return runningConfig.Data.Forwards.Where(x => x.GroupId == groupid).ToList();
        }

        public bool Add(ForwardInfo info)
        {
            runningConfig.Data.Forwards.Add(info);
            return true;
        }
        public bool Update(ForwardInfo info)
        {
            return true;
        }
        public bool Remove(uint id)
        {
            runningConfig.Data.Forwards.Remove(Get(id));
            return true;
        }

        public bool Confirm()
        {
            runningConfig.Data.Update();
            return true;
        }


    }
}
