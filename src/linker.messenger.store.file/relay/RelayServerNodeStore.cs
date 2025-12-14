using linker.messenger.relay.server;
using LiteDB;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerNodeStore : IRelayNodeStore
    {
        private readonly ILiteCollection<RelayServerNodeStoreInfo> liteCollection;
        public RelayServerNodeStore(Storefactory storefactory)
        {
            liteCollection = storefactory.GetCollection<RelayServerNodeStoreInfo>($"relay_server_master");
        }

        public async Task<bool> Add(RelayServerNodeStoreInfo info)
        {
            if (liteCollection.FindOne(c => c.NodeId == info.NodeId) != null)
            {
                return false;
            }
            liteCollection.Insert(info);
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<bool> Delete(string nodeId)
        {
            return await Task.FromResult(liteCollection.DeleteMany(c => c.NodeId == nodeId) > 0).ConfigureAwait(false);
        }

        public async Task<List<RelayServerNodeStoreInfo>> GetAll()
        {
            return await Task.FromResult(liteCollection.FindAll().ToList()).ConfigureAwait(false);
        }

        public async Task<RelayServerNodeStoreInfo> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(liteCollection.FindOne(c => c.NodeId == nodeId)).ConfigureAwait(false);
        }
        public async Task<bool> Report(RelayServerNodeReportInfo info)
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
                MasterCount = info.MasterCount,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public async Task<bool> Update(RelayServerNodeStoreInfo info)
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
