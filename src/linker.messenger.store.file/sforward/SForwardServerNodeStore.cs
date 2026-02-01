using linker.libs.extends;
using linker.messenger.sforward.server;
using LiteDB;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerNodeStore : ISForwardNodeStore
    {
        private readonly ILiteCollection<SForwardServerNodeStoreInfo> liteCollection;

        private List<SForwardServerNodeStoreInfo> list = new List<SForwardServerNodeStoreInfo>();
        public SForwardServerNodeStore(Storefactory storefactory)
        {
            liteCollection = storefactory.GetCollection<SForwardServerNodeStoreInfo>($"sforward_server_master");
            LoadList();
        }

        public async Task<bool> Add(SForwardServerNodeStoreInfo info)
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

        public async Task<List<SForwardServerNodeStoreInfo>> GetAll()
        {
            return await Task.FromResult(list).ConfigureAwait(false);
        }

        public async Task<SForwardServerNodeStoreInfo> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(list.FirstOrDefault(c => c.NodeId == nodeId)).ConfigureAwait(false);
        }
        public async Task<bool> Report(SForwardServerNodeReportInfo info)
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
            LoadList();
            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public async Task<bool> Update(SForwardServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new SForwardServerNodeStoreInfo
            {
                BandwidthEach = info.BandwidthEach,
                Public = info.Public,
                Host = info.Host,
            }, c => c.NodeId == info.NodeId);
            LoadList();
            return await Task.FromResult(length > 0).ConfigureAwait(false); ;
        }

        private void LoadList()
        {
            list = liteCollection.FindAll().ToList();
        }
    }
}
