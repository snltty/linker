namespace cmonitor.server.ruleConfig
{
    public sealed class RuleConfigMacOS : IRuleConfig
    {
        public RuleConfigInfo Data => null;

        public void AddUser(string username)
        {

        }

        public T Get<T>(string username, string key, T defaultValue)
        {
            return defaultValue;
        }

        public void Set<T>(string username, string key, T data)
        {

        }
    }
}
