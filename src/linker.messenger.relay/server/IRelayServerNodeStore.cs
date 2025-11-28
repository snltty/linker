namespace linker.messenger.relay.server
{
    public interface IRelayServerNodeStore
    {
        public Task<List<RelayServerNodeStoreInfo>> GetAll();
        public Task<RelayServerNodeStoreInfo> GetByNodeId(string nodeId);
        public Task<bool> Add(RelayServerNodeStoreInfo info);
        public Task<bool> Report(RelayServerNodeReportInfo info);
    }

    public sealed class RelayServerNodeStoreInfo : RelayServerNodeReportInfo
    {
        public int Id { get; set; }

        public int BandwidthEachConnection { get; set; } = 50;
        public bool Public { get; set; }

        public long LastTicks { get; set; }

        public int Delay { get; set; }
    }
}
