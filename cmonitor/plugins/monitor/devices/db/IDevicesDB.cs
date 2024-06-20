using LiteDB;

namespace cmonitor.plugins.devices.db
{
    public interface IDevicesDB
    {
        public bool Add(DevicesUserInfo devicesUserInfo);
        public List<DevicesUserInfo> Get();
    }

    public sealed class DevicesUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<string> Data { get; set; } = new List<string>();
    }

}
