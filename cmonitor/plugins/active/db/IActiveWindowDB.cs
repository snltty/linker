using LiteDB;

namespace cmonitor.plugins.active.db
{
    public interface IActiveWindowDB
    {
        public bool Add(WindowUserInfo windowUserInfo);
        public List<WindowUserInfo> Get();
    }

    public sealed class WindowUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<WindowGroupInfo> Data { get; set; } = new List<WindowGroupInfo>();
    }
    public sealed class WindowGroupInfo
    {
        public string Name { get; set; }
        public List<WindowItemInfo> List { get; set; } = new List<WindowItemInfo>();
    }
    public sealed class WindowItemInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
    }

}
