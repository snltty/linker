using linker.messenger.sforward.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerNodeStore : NodeStore<SForwardServerNodeStoreInfo, SForwardServerNodeReportInfo>, ISForwardNodeStore
    {
        public override string StoreName => "sforward";
        public SForwardServerNodeStore(Storefactory storefactory) : base(storefactory)
        {
        }

        public override async Task<bool> Report(SForwardServerNodeReportInfo info)
        {
            int length = liteCollection.UpdateMany(p => new SForwardServerNodeStoreInfo
            {
                LastTicks = Environment.TickCount64,
                Bandwidth = info.Bandwidth,
                Connections = info.Connections,
                Version = info.Version,
                ConnectionsRatio = info.ConnectionsRatio,
                BandwidthRatio = info.BandwidthRatio,
                Url = info.Url,
                Logo = info.Logo,
                DataEachMonth = info.DataEachMonth,
                DataRemain = info.DataRemain,
                Name = info.Name,
                MasterCount = info.MasterCount,
                Domain = info.Domain,
                WebPort = info.WebPort,
                TunnelPorts = info.TunnelPorts,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public override async Task<bool> Update(SForwardServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new SForwardServerNodeStoreInfo
            {
                BandwidthEach = info.BandwidthEach,
                Public = info.Public,
                Host = info.Host,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false); ;
        }
    }
}
