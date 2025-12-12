using linker.libs.extends;
using linker.messenger.node;
using linker.messenger.relay.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerNodeStore : NodeStore<RelayServerNodeStoreInfo, RelayServerNodeReportInfo>, IRelayNodeStore
    {
        public override string StoreName => "relay";
        private string md5 = string.Empty;
        public RelayServerNodeStore(Storefactory storefactory, IRelayNodeConfigStore relayServerConfigStore) : base(storefactory)
        {
            md5 = relayServerConfigStore.Config.NodeId.Md5();
        }

        public override async Task<bool> Report(RelayServerNodeReportInfo info)
        {
            int length = liteCollection.UpdateMany(p => new RelayServerNodeStoreInfo
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
                Protocol = info.Protocol,
                MasterKey = info.MasterKey,
                MasterCount = info.MasterCount,
                //是我初始化的，可以管理
                Manageable = info.MasterKey == md5
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public override async Task<bool> Update(RelayServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new RelayServerNodeStoreInfo
            {
                Public = info.Public,
                Host = info.Host,
                BandwidthEach = info.BandwidthEach,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false); ;
        }
    }
}
