using common.libs;
using common.libs.database;
using common.libs.extends;

namespace cmonitor.server.ruleConfig
{
    //[SupportedOSPlatform("windows")]

    public sealed class RuleConfigWindows : IRuleConfig
    {
        private readonly IConfigDataProvider<RuleConfigInfo> configDataProvider;
        private readonly RuleConfigInfo RuleConfigInfo;
        private Dictionary<string, Dictionary<string, object>> cache = new Dictionary<string, Dictionary<string, object>>();

        public RuleConfigInfo Data => RuleConfigInfo;


        public RuleConfigWindows(IConfigDataProvider<RuleConfigInfo> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            RuleConfigInfo = configDataProvider.Load().Result ?? new RuleConfigInfo { };
            if (RuleConfigInfo.Data.Keys.Count == 0)
            {
                RuleConfigInfo.Data.Add("snltty", new Dictionary<string, string>());
            }
        }

        public void AddUser(string username)
        {
            if (RuleConfigInfo.Data.ContainsKey(username))
            {
                return;
            }
            RuleConfigInfo.Data.Add(username, new Dictionary<string, string>());
        }

        public T Get<T>(string username, string key, T defaultValue)
        {

            try
            {
                if (cache.TryGetValue(username, out Dictionary<string, object> dic) && dic.TryGetValue(key, out object cacheValue))
                {
                    return (T)cacheValue;
                }

                if (RuleConfigInfo.Data.TryGetValue(username, out Dictionary<string, string> dicStr) && dicStr.TryGetValue(key, out string value))
                {
                    if (string.IsNullOrWhiteSpace(value) == false)
                    {
                        T data = value.DeJson<T>();
                        cache[username][key] = data;
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

            return defaultValue;
        }

        public void Set<T>(string username, string key, T data)
        {
            try
            {
                string value = data.ToJson();
                if (RuleConfigInfo.Data.TryGetValue(username, out Dictionary<string, string> dicStr) == false)
                {
                    RuleConfigInfo.Data[username] = new Dictionary<string, string>();
                }

                RuleConfigInfo.Data[username][key] = value;

                if (cache.TryGetValue(username, out Dictionary<string, object> dic) == false)
                {
                    cache[username] = new Dictionary<string, object>();
                }
                cache[username][key] = data;

                configDataProvider.Save(RuleConfigInfo).Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }


}
