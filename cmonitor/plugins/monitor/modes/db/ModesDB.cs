using cmonitor.db;
using LiteDB;

namespace cmonitor.plugins.modes.db
{
    public sealed class ModesDB : IModesDB
    {
        ILiteCollection<ModesUserInfo> collection;
        private readonly DBfactory dBfactory;
        public ModesDB(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            collection = dBfactory.GetCollection<ModesUserInfo>("modes");
        }

        public bool Add(ModesUserInfo modesUserInfo)
        {
            ModesUserInfo old = collection.FindOne(c => c.UserName == modesUserInfo.UserName);
            if (old != null)
            {
                old.Data = modesUserInfo.Data;
                collection.Update(old);
            }
            else
            {
                collection.Insert(modesUserInfo);
            }
            dBfactory.Confirm();
            return true;
        }

        public List<ModesUserInfo> Get()
        {
            return collection.FindAll().ToList();
        }
    }
}
