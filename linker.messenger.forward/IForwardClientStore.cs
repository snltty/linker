namespace linker.messenger.forward
{
    public interface IForwardClientStore
    {
        public int Count();

        public List<ForwardInfo> Get();
        public ForwardInfo Get(uint id);
        public List<ForwardInfo> Get(string groupId);
        public bool Add(ForwardInfo info);
        public bool Update(ForwardInfo info);
        public bool Remove(uint id);

        public bool Confirm();
    }
}
