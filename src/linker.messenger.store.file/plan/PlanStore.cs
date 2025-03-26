using linker.messenger.plan;
using LiteDB;

namespace linker.messenger.store.file.plan
{
    public sealed class PlanStore : IPlanStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<PlanStoreInfo> liteCollection;
        public PlanStore(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<PlanStoreInfo>("plan");
        }
        public bool Add(PlanStoreInfo info)
        {
            if (info.Id == 0)
            {
                if (liteCollection.FindOne(c => c.Category == info.Category && c.Key == info.Key && c.Handle == info.Handle && c.Method == info.Method && c.Rule == info.Rule) != null)
                {
                    return false;
                }
                liteCollection.Insert(info);
                return true;
            }

            return liteCollection.Update(info);
        }

        public IEnumerable<PlanStoreInfo> Get()
        {
            return liteCollection.FindAll();
        }

        public IEnumerable<PlanStoreInfo> Get(string category)
        {
            return liteCollection.Find(c => c.Category == category);
        }
        public PlanStoreInfo Get(string category, string key)
        {
            return liteCollection.FindOne(c => c.Category == category && c.Key == key);
        }

        public bool Remove(int id)
        {
            return liteCollection.Delete(id);
        }
    }
}
