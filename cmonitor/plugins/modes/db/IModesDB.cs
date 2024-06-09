using LiteDB;

namespace cmonitor.plugins.modes.db
{
    public interface IModesDB
    {
        public bool Add(ModesUserInfo modesUserInfo);
        public List<ModesUserInfo> Get();
    }

    public sealed class ModesUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<ModesInfo> Data { get; set; } = new List<ModesInfo>();
    }
    public sealed class ModesInfo
    {
        public string Name { get; set; }
        public string Data { get; set; }
    }


}
