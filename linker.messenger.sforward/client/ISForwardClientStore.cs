namespace linker.messenger.sforward.client
{
    public interface ISForwardClientStore
    {
        public string SecretKey { get;  }

        public bool SetSecretKey(string key);

        public int Count();

        public List<SForwardInfo> Get();
        public SForwardInfo Get(uint id);
        public SForwardInfo Get(string domain);
        public SForwardInfo Get(int port);
        public bool Add(SForwardInfo info);
        public bool Update(SForwardInfo info);
        public bool Remove(uint id);

        public bool Confirm();
    }
}
