using linker.messenger.wakeup;
using LiteDB;

namespace linker.messenger.store.file.wakeup
{

    public sealed class WakeupClientStore : IWakeupClientStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<WakeupInfo> liteCollection;
        private readonly RunningConfig runningConfig;
        public WakeupClientStore(Storefactory dBfactory, RunningConfig runningConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<WakeupInfo>("wakeup");
            this.runningConfig = runningConfig;
        }
        public IEnumerable<WakeupInfo> GetAll(WakeupSearchInfo info)
        {
            IEnumerable<WakeupInfo> list = liteCollection.FindAll().Where(c => (c.Type & info.Type) > 0);

            if (string.IsNullOrWhiteSpace(info.Str) == false)
            {
                list = list.Where(c =>
                (c.Name != null && c.Name.Contains(info.Str)) ||
                (c.Value != null && c.Value.Contains(info.Str)) ||
                (c.Remark != null && c.Remark.Contains(info.Str))
                );
            }
            return list;
        }

        public bool Add(WakeupInfo rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                rule.Id = ObjectId.NewObjectId().ToString();
                return liteCollection.Insert(rule) != null;
            }
            else
            {
                return liteCollection.UpdateMany(p => new WakeupInfo
                {
                    Name = rule.Name,
                    Type = rule.Type,
                    Value = rule.Value,
                    Content = rule.Content,
                    Remark = rule.Remark
                }, c => c.Id == rule.Id) > 0;
            }
        }

        public bool Remove(string id)
        {
            return liteCollection.Delete(id);
        }

        public int Count()
        {
            return liteCollection.Count();
        }
    }
}
