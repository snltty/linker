using linker.libs.extends;
using linker.messenger.sforward.server;
using LiteDB;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerNodeStore : ISForwardServerNodeStore
    {
        private readonly ILiteCollection<SForwardServerNodeStoreInfo> liteCollection;

        private string md5 = string.Empty;
        public SForwardServerNodeStore(Storefactory storefactory, ISForwardServerConfigStore SForwardServerConfigStore)
        {
            liteCollection = storefactory.GetCollection<SForwardServerNodeStoreInfo>("SForward_server_master");
            md5 = SForwardServerConfigStore.Config.NodeId.Md5();
        }

        public async Task<bool> Add(SForwardServerNodeStoreInfo info)
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

        public async Task<List<SForwardServerNodeStoreInfo>> GetAll()
        {
            return await Task.FromResult(liteCollection.FindAll().ToList()).ConfigureAwait(false);
        }

        public async Task<SForwardServerNodeStoreInfo> GetByNodeId(string nodeId)
        {
            return await Task.FromResult(liteCollection.FindOne(c => c.NodeId == nodeId)).ConfigureAwait(false);
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
                MasterKey = info.MasterKey,
                Masters = info.Masters,
                //是我初始化的，可以管理
                Manageable = info.MasterKey == md5
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false);
        }

        public async Task<bool> Update(SForwardServerNodeStoreInfo info)
        {
            int length = liteCollection.UpdateMany(p => new SForwardServerNodeStoreInfo
            {
                DataEachMonth = info.DataEachMonth,
                Public = info.Public,
            }, c => c.NodeId == info.NodeId);

            return await Task.FromResult(length > 0).ConfigureAwait(false); ;
        }
    }
}
