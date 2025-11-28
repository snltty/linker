using linker.messenger.relay.server;
using LiteDB;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerNodeStore : IRelayServerNodeStore
    {
        private readonly ILiteCollection<RelayServerNodeStoreInfo> liteCollection;

        public RelayServerNodeStore(Storefactory storefactory)
        {
            liteCollection  = storefactory.GetCollection<RelayServerNodeStoreInfo>("relay_server_master");
        }

        public async Task<bool> Add(RelayServerNodeStoreInfo info)
        {
            if(liteCollection.FindOne(c=>c.NodeId == info.NodeId) != null)
            {
                return false;
            }
            liteCollection.Insert(info);
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<List<RelayServerNodeStoreInfo>> GetAll()
        {
            return await Task.FromResult(liteCollection.FindAll().ToList()).ConfigureAwait(false);
        }

        public async Task<RelayServerNodeStoreInfo> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(liteCollection.FindOne(c => c.NodeId == nodeId)).ConfigureAwait(false);
        }
    }
}
