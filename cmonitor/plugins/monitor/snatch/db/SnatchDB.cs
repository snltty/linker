using cmonitor.db;
using LiteDB;

namespace cmonitor.plugins.snatch.db
{
    public sealed class SnatchDB : ISnatchDB
    {
        ILiteCollection<SnatchUserInfo> collection;
        private readonly DBfactory dBfactory;
        public SnatchDB(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            collection = dBfactory.GetCollection<SnatchUserInfo>("snatch");
        }

        public bool Add(SnatchUserInfo  snatchUserInfo)
        {
            SnatchUserInfo old = collection.FindOne(c => c.UserName == snatchUserInfo.UserName);
            if (old != null)
            {
                old.Data = snatchUserInfo.Data;
                collection.Update(old);
            }
            else
            {
                collection.Insert(snatchUserInfo);
            }
            dBfactory.Confirm();
            return true;
        }

        public List<SnatchUserInfo> Get()
        {
            return collection.FindAll().ToList();
        }
    }
}
