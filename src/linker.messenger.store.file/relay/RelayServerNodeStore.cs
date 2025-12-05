using linker.libs.extends;
using linker.messenger.relay.server;
using LiteDB;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerNodeStore : IRelayServerNodeStore
    {
        private readonly ILiteCollection<RelayServerNodeStoreInfo> liteCollection;

        private string md5 = string.Empty;
        public RelayServerNodeStore(Storefactory storefactory, IRelayServerConfigStore relayServerConfigStore)
        {
            liteCollection = storefactory.GetCollection<RelayServerNodeStoreInfo>("relay_server_master");
            md5 = relayServerConfigStore.Config.NodeId.Md5();
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
                MasterKey = info.MasterKey,
                Masters = info.Masters,
                //是我初始化的，可以管理
                Manageable = info.MasterKey == md5
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public async Task<bool> Update(RelayServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new RelayServerNodeStoreInfo
            {
                DataEachMonth = info.DataEachMonth,
                Public = info.Public,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false); ;
        }
    }
}
