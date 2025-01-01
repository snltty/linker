using linker.messenger.tuntap.lease;
using LiteDB;

namespace linker.messenger.store.file.tuntap
{
    public sealed class LeaseServerStore : ILeaseServerStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<LeaseCacheInfo> liteCollection;
        public LeaseServerStore(Storefactory dBfactory)
        {
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
            dBfactory.Confirm();
            return true;
        }

        public List<LeaseCacheInfo> Get()
        {
            return liteCollection.FindAll().ToList();
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
