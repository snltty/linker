namespace linker.messenger.tuntap.lease
{
    public interface ILeaseClientStore
    {
        public LeaseInfo Get(string key);
        public bool Set(string key,LeaseInfo info);
        public void Confirm();
    }
}
