using LiteDB;

namespace cmonitor.plugins.hijack.db
{
    public interface IHijackDB
    {
        public bool AddRule(HijackRuleUserInfo hijackRuleUserInfo);
        public List<HijackRuleUserInfo> GetRule();

        public bool AddProcess(HijackProcessUserInfo hijackProcessUserInfo);
        public List<HijackProcessUserInfo> GetProcess();
    }

    public sealed class HijackRuleUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<RulesInfo> Data { get; set; }
    }
    public sealed class RulesInfo
    {
        public string Name { get; set; }
        public List<string> PrivateProcesss { get; set; } = new List<string>();
        public List<string> PublicProcesss { get; set; } = new List<string>();
    }


    public sealed class HijackProcessUserInfo
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public List<HijackProcessGroupInfo> Data { get; set; }
    }
    public sealed class HijackProcessGroupInfo
    {
        public string Name { get; set; }
        public List<HijackProcessItemInfo> List { get; set; } = new List<HijackProcessItemInfo>();
    }
    public sealed class HijackProcessItemInfo
    {
        public string Name { get; set; }
        public HijackProcessDataType DataType { get; set; }
        public HijackProcessAllowType AllowType { get; set; }
    }
    public enum HijackProcessDataType
    {
        Process = 0,
        Domain = 1,
        IP = 2,
    }
    public enum HijackProcessAllowType
    {
        Allow = 0,
        Denied = 1
    }


}
