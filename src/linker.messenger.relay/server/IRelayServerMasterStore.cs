namespace linker.messenger.relay.server
{
    public interface IRelayServerMasterStore
    {
        public Task<List<RelayNodeStoreInfo>> GetAll();
    }

    public sealed class RelayNodeStoreInfo : RelayServerNodeReportInfo
    {
        public int Id { get; set; }

        public int BandwidthEachConnection { get; set; } = 50;
        public bool Public { get; set; }

        public int Delay { get; set; }
        public long LastTicks { get; set; }


    }
}
