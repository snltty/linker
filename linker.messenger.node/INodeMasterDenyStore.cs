namespace linker.messenger.node
{
    public interface INodeMasterDenyStore
    {
        public Task<MasterDenyStoreResponseInfo> Get(MasterDenyStoreRequestInfo request);
        public Task<bool> Get(uint ip);
        public Task<bool> Add(string str);
        public Task<bool> Delete(int id);
    }

    public sealed class MasterDenyStoreRequestInfo
    {
        public int Page { get; set; }
        public int Sise { get; set; }
        public string Str { get; set; }
    }
    public sealed class MasterDenyStoreResponseInfo
    {
        public int Page { get; set; }
        public int Sise { get; set; }
        public int Count { get; set; }
        public List<MasterDenyStoreInfo> List { get; set; } = new List<MasterDenyStoreInfo>();
    }
    public sealed class MasterDenyStoreInfo
    {
        public int Id { get; set; }
        public uint Ip { get; set; }
        public uint Plus { get; set; }
        public string Str { get; set; }
    }

}
