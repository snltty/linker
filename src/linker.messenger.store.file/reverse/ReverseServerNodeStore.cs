using linker.messenger.reverse.server;
using LiteDB;

namespace linker.messenger.store.file.reverse
{
    public sealed class ReverseServerNodeStore : IReverseNodeStore
    {
        private readonly ILiteCollection<ReverseServerNodeStoreInfo> liteCollection;

        private List<ReverseServerNodeStoreInfo> list = new List<ReverseServerNodeStoreInfo>();
        public ReverseServerNodeStore(Storefactory storefactory)
        {
            liteCollection = storefactory.GetCollection<ReverseServerNodeStoreInfo>($"sforward_server_master");
            LoadList();
        }

        public async Task<bool> Add(ReverseServerNodeStoreInfo info)
        {
            if (liteCollection.FindOne(c => c.NodeId == info.NodeId) != null)
            {
                return false;
            }
            liteCollection.Insert(info);
            LoadList();
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<bool> Delete(string nodeId)
        {
            bool result = await Task.FromResult(liteCollection.DeleteMany(c => c.NodeId == nodeId) > 0).ConfigureAwait(false);
            LoadList();
            return result;
        }

        public async Task<List<ReverseServerNodeStoreInfo>> GetAll()
        {
            return await Task.FromResult(list).ConfigureAwait(false);
        }

        public async Task<ReverseServerNodeStoreInfo> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(list.FirstOrDefault(c => c.NodeId == nodeId)).ConfigureAwait(false);
        }
        public async Task<bool> Report(ReverseServerNodeReportInfo info)
        {
            int length = liteCollection.UpdateMany(p => new ReverseServerNodeStoreInfo
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
            LoadList();
            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public async Task<bool> Update(ReverseServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new ReverseServerNodeStoreInfo
            {
                BandwidthEach = info.BandwidthEach,
                Public = info.Public,
                Host = info.Host,
            }, c => c.NodeId == info.NodeId);
            LoadList();
            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        private void LoadList()
        {
            list = liteCollection.FindAll().ToList();
        }
    }
}
