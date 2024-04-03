using System.ComponentModel.DataAnnotations.Schema;

namespace cmonitor.server.ruleConfig
{
    public interface IRuleConfig
    {
        public RuleConfigInfo Data { get; }

        public void AddUser(string username);

        public T Get<T>(string username,string key, T defaultValue);
        public void Set<T>(string username, string key, T data);
    }

    [Table("rule")]
    public sealed class RuleConfigInfo
    {
        public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}
