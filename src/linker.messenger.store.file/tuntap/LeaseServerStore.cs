using linker.messenger.tuntap;
using linker.messenger.tuntap.lease;
using LiteDB;

namespace linker.messenger.store.file.tuntap
{
    public sealed class LeaseServerStore : ILeaseServerStore
    {
        public TuntapLeaseConfigServerInfo Info => fileConfig.Data.Server.Tuntap.Lease;

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<LeaseCacheInfo> liteCollection;
        private readonly FileConfig fileConfig;
        public LeaseServerStore(FileConfig fileConfig,Storefactory dBfactory)
        {
            this.fileConfig = fileConfig;
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<LeaseCacheInfo>("dhcp");
           
        }
        public bool Add(LeaseCacheInfo info)
        {
            info.Id = ObjectId.NewObjectId().ToString();
            liteCollection.Insert(info);
            return true;
        }

        public bool Confirm()
        {
            return true;
        }

        public List<LeaseCacheInfo> Get()
        {
            try
            {
                return liteCollection.FindAll().ToList();
            }
            catch (Exception)
            {
            }
            return new List<LeaseCacheInfo>();
        }

        public bool Remove(string id)
        {
            return liteCollection.Delete(id);
        }

        public bool Update(LeaseCacheInfo info)
        {
            return liteCollection.Update(info);
        }
    }
}
