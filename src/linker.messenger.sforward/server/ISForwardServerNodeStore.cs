namespace linker.messenger.sforward.server
{
    public interface ISForwardServerNodeStore
    {
        public Task<List<SForwardServerNodeStoreInfo>> GetAll();
        public Task<SForwardServerNodeStoreInfo> GetByNodeId(string nodeId);
        public Task<bool> Add(SForwardServerNodeStoreInfo info);
        public Task<bool> Report(SForwardServerNodeReportInfo info);
        public Task<bool> Delete(string nodeId);
        public Task<bool> Update(SForwardServerNodeStoreInfo info);
    }
}
