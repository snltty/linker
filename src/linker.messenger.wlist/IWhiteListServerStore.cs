namespace linker.messenger.wlist
{
    public interface IWhiteListServerStore
    {
        public WhiteListConfigServerInfo Config { get; }

        public Task<WhiteListPageResultInfo> Page(WhiteListPageRequestInfo request);
        public Task<bool> Add(WhiteListInfo info);
        public Task<bool> Del(int id);

        public Task<List<WhiteListInfo>> Get(string type, List<string> userids, List<string> machineIds);
        public Task<WhiteListInfo> Get(string tradeNo);
    }
    public sealed partial class WhiteListDelInfo
    {
        public int Id { get; set; }
    }
    public sealed partial class WhiteListAddInfo
    {
        public WhiteListInfo Data { get; set; }
    }
    public sealed partial class WhiteListPageRequestInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string Type { get; set; }
        public string MachineId { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
    }
    public sealed partial class WhiteListPageResultInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<WhiteListInfo> List { get; set; }
    }
    public sealed partial class WhiteListInfo
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime AddTime { get; set; } = DateTime.Now;
        public DateTime UseTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now.AddYears(1);

        public string TradeNo { get; set; } = string.Empty;

        public string[] Nodes { get; set; } = [];

        public double Bandwidth { get; set; } = 0;


    }

    public sealed class WhiteListOrderStatusInfo
    {
        public bool Enabled { get; set; }
        public string Type { get; set; } = string.Empty;
        public WhiteListInfo Info { get; set; }

    }
}
