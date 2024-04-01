using common.libs;
using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Versioning;

namespace cmonitor.client.runningConfig
{
    [SupportedOSPlatform("windows")]

    public sealed class RunningConfigWindows : IRunningConfig
    {
        private readonly IConfigDataProvider<RunningConfigInfo> configDataProvider;
        private readonly RunningConfigInfo runningConfigInfo;

        private Dictionary<string, object> cache = new Dictionary<string, object>();


        public RunningConfigWindows(IConfigDataProvider<RunningConfigInfo> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            runningConfigInfo = configDataProvider.Load().Result ?? new RunningConfigInfo();
        }

        public T Get<T>(T defaultValue)
        {
            try
            {
                string name = typeof(T).Name;
                if (cache.TryGetValue(name, out object cacheValue))
                {
                    return (T)cacheValue;
                }

                if (runningConfigInfo.Running.TryGetValue(name, out string value))
                {
                    if (string.IsNullOrWhiteSpace(value) == false)
                    {
                        T data = value.DeJson<T>();
                        cache[name] = data;
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

        public void Set<T>(T data)
        {
            try
            {
                string name = typeof(T).Name;
                string value = data.ToJson();
                runningConfigInfo.Running[name] = value;
                cache[name] = data;

                configDataProvider.Save(runningConfigInfo).Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }

    [Table("running")]
    public sealed class RunningConfigInfo
    {
        public Dictionary<string, string> Running { get; set; } = new Dictionary<string, string>();
    }
}
