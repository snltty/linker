using cmonitor.db;
using LiteDB;

namespace cmonitor.plugins.devices.db
{
    public sealed class DevicesDB : IDevicesDB
    {
        ILiteCollection<DevicesUserInfo> collection;
        private readonly DBfactory dBfactory;
        public DevicesDB(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            collection = dBfactory.GetCollection<DevicesUserInfo>("devices");
        }

        public bool Add(DevicesUserInfo devicesUserInfo)
        {
            DevicesUserInfo old = collection.FindOne(c => c.UserName == devicesUserInfo.UserName);
            if (old != null)
            {
                old.Data = devicesUserInfo.Data;
                collection.Update(old);
            }
            else
            {
                collection.Insert(devicesUserInfo);
            }
            dBfactory.Confirm();
            return true;
        }

        public List<DevicesUserInfo> Get()
        {
            return collection.FindAll().ToList();
        }
    }
}
