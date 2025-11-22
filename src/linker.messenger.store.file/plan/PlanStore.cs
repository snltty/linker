using linker.messenger.plan;
using LiteDB;

namespace linker.messenger.store.file.plan
{
    public sealed class PlanStore : IPlanStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<PlanInfo> liteCollection;
        public PlanStore(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<PlanInfo>("plan");
        }
        public bool Add(PlanInfo info)
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

        public IEnumerable<PlanInfo> Get()
        {
            return liteCollection.FindAll().ToList();
        }

        public IEnumerable<PlanInfo> Get(string category)
        {
            return liteCollection.Find(c => c.Category == category).ToList();
        }
        public PlanInfo Get(string category, string key)
        {
            return liteCollection.FindOne(c => c.Category == category && c.Key == key);
        }

        public bool Remove(int id)
        {
            return liteCollection.Delete(id);
        }
    }
}
