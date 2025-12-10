using linker.messenger.node;
using LiteDB;

namespace linker.messenger.store.file.node
{
    public class NodeStore<TStore, TReport> : INodeStore<TStore, TReport>
        where TStore : class, INodeStoreBase, new()
        where TReport : class, INodeReportBase, new()
    {

        public virtual string StoreName => "relay";

        protected readonly ILiteCollection<TStore> liteCollection;

        public NodeStore(Storefactory storefactory)
        {
            liteCollection = storefactory.GetCollection<TStore>($"{StoreName}_server_master");
        }

        public async Task<bool> Add(TStore info)
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

        public async Task<List<TStore>> GetAll()
        {
            return await Task.FromResult(liteCollection.FindAll().ToList()).ConfigureAwait(false);
        }

        public async Task<TStore> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(liteCollection.FindOne(c => c.NodeId == nodeId)).ConfigureAwait(false);
        }

        public virtual async Task<bool> Report(TReport info)
        {
            return false;
        }
        public virtual async Task<bool> Update(TStore info)
        {
            return false;
        }

    }
}
