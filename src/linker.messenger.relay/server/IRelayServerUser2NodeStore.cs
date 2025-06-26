namespace linker.messenger.relay.server
{
    public interface IRelayServerUser2NodeStore
    {
        public Task<RelayServerUser2NodePageResultInfo> Page(RelayServerUser2NodePageRequestInfo request);
        public Task<bool> Add(RelayServerUser2NodeInfo info);
        public Task<bool> Del(int id);

        public Task<List<string>> Get(string userid);
    }
    public sealed partial class RelayServerUser2NodeDelInfo
    {
        public string SecretKey { get; set; }
        public int Id { get; set; }
    }
    public sealed partial class RelayServerUser2NodeAddInfo
    {
        public string SecretKey { get; set; }
        public RelayServerUser2NodeInfo Data { get; set; }
    }
    public sealed partial class RelayServerUser2NodePageRequestInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string SecretKey { get; set; }
    }
    public sealed partial class RelayServerUser2NodePageResultInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<RelayServerUser2NodeInfo> List { get; set; }
    }
    public sealed partial class RelayServerUser2NodeInfo
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime AddTime { get; set; } = DateTime.Now;

        public string[] Nodes { get; set; } = [];
    }
}
