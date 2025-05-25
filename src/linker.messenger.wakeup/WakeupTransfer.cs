namespace linker.messenger.wakeup
{
    public sealed class WakeupTransfer
    {
        private readonly IWakeupClientStore wakeupClientStore;
        public WakeupTransfer(IWakeupClientStore wakeupClientStore)
        {
            this.wakeupClientStore = wakeupClientStore;
        }

        public List<WakeupInfo> Get(WakeupSearchInfo info)
        {
            return wakeupClientStore.GetAll(info).ToList();
        }
        public bool Add(WakeupInfo info)
        {
            wakeupClientStore.Add(info);
            return true;
        }
        public bool Remove(string id)
        {
            wakeupClientStore.Remove(id);
            return true;
        }
    }
}
