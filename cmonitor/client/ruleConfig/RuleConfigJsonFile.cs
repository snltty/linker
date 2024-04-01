namespace cmonitor.client.ruleConfig
{
    /*
    public sealed class RuleConfigJsonFile : IRuleConfig
    {
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public RuleConfigJsonFile()
        {
        }

        public T Get<T>(T defaultValue)
        {
            string name = nameof(T);
            return Get(name, defaultValue);

        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                if (cache.TryGetValue(name, out object cacheValue))
                {
                    return (T)cacheValue;
                }

                string value = (Registry.GetValue(savePath, name, string.Empty)).ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    T data = value.DeJson<T>();
                    cache[name] = data;
                    return data;
                }
            }
            catch (Exception)
            {
            }
            return defaultValue;
        }

        public void Set<T>(T data)
        {
            try
            {
                string name = nameof(T);
                string value = data.ToJson();
                Registry.SetValue(savePath, name, value);
                cache[name] = data;
            }
            catch (Exception)
            {
            }
        }
    }

    [Table("rule-config")]
    public sealed class RuleConfig
    {
        private readonly IConfigDataProvider<RuleConfig> configDataProvider;
        public RuleConfig() { }
        public RuleConfig(IConfigDataProvider<RuleConfig> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            RuleConfig config = configDataProvider.Load().Result ?? new RuleConfig
            {
                UserNames = new Dictionary<string, string> { { "snltty", "{}" } }
            };
            UserNames = config.UserNames;
        }

        public Dictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();

    }*/
}
