namespace linker.messenger.relay.server
{
    public interface IRelayServerNodeStore
    {
        public Task<List<RelayServerNodeStoreInfo>> GetAll();
        public Task<RelayServerNodeStoreInfo> GetByNodeId(string nodeId);
        public Task<bool> Add(RelayServerNodeStoreInfo info);
        public Task<bool> Report(RelayServerNodeReportInfo info);
    }

  
}
