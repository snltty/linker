using cmonitor.db;
using LiteDB;
using SharpDX.DXGI;

namespace cmonitor.plugins.active.db
{
    public sealed class ActiveWindowDB : IActiveWindowDB
    {
        ILiteCollection<WindowUserInfo> collection;
        private readonly DBfactory dBfactory;
        public ActiveWindowDB(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            collection = dBfactory.GetCollection<WindowUserInfo>("activewindow");
        }

        public bool Add(WindowUserInfo windowUserInfo)
        {
            WindowUserInfo old = collection.FindOne(c => c.UserName == windowUserInfo.UserName);
            if (old != null)
            {
                old.Data = windowUserInfo.Data;
                collection.Update(old);
            }
            else
            {
                collection.Insert(windowUserInfo);
            }
            dBfactory.Confirm();
            return true;
        }

        public List<WindowUserInfo> Get()
        {
            return collection.FindAll().ToList();
        }
    }
}
