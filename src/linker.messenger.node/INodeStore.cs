namespace linker.messenger.node
{
    public interface INodeStoreBase
    {
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public bool Manageable { get; set; }
        public string MasterKey { get; set; }
        public string ShareKey { get; set; }
        public long LastTicks { get; set; }
    }
    public interface INodeReportBase
    {
        public string NodeId { get; set; }
        public string Name { get; set; }

        public int Connections { get; set; }
        public int Bandwidth { get; set; }
        public int DataEachMonth { get; set; }
        public long DataRemain { get; set; }

        public string Url { get; set; }
        public string Logo { get; set; }

        public string MasterKey { get; set; }
        public string Version { get; set; }
        public int ConnectionsRatio { get; set; }
        public double BandwidthRatio { get; set; }

        public int MasterCount { get; set; }

        public string Host { get; set; }
    }
    public interface INodeStore<TStore, TReport> 
        where TStore : class, INodeStoreBase, new() 
        where TReport : class, INodeReportBase, new()
    {
        public Task<List<TStore>> GetAll();
        public Task<TStore> GetByNodeId(string nodeId);
        public Task<bool> Add(TStore info);
        public Task<bool> Report(TReport info);
        public Task<bool> Delete(string nodeId);
        public Task<bool> Update(TStore info);
    }


}
