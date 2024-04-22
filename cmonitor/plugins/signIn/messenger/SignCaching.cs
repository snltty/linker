using cmonitor.server;
using common.libs.database;
using MemoryPack;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace cmonitor.plugins.signin.messenger
{
    public sealed class SignCaching
    {
        private readonly IConfigDataProvider<SignCacheFileInfo> configDataProvider;
        private SignCacheFileInfo config;
        private bool changed = false;

        public SignCaching(IConfigDataProvider<SignCacheFileInfo> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            config = configDataProvider.Load().Result ?? new SignCacheFileInfo();
            SaveConfig();
        }

        public void Sign(IConnection connection, SignInfo signInfo)
        {
            if (config.Clients.TryRemove(signInfo.MachineName, out SignCacheInfo cache))
            {
                cache.Connection?.Disponse();
            }
            connection.Name = signInfo.MachineName;
            SignCacheInfo cache1 = new SignCacheInfo
            {
                Connection = connection,
                MachineName = signInfo.MachineName,
                Version = signInfo.Version,
                Args = signInfo.Args,
            };
            config.Clients.TryAdd(signInfo.MachineName, cache1);
            changed = true;
        }
        public bool Get(string machineName, out SignCacheInfo cache)
        {
            return config.Clients.TryGetValue(machineName, out cache);
        }
        public List<SignCacheInfo> Get()
        {
            return config.Clients.Values.ToList();
        }

        public bool Del(string machineName)
        {
            bool res = config.Clients.TryRemove(machineName, out _);
            changed = true;
            return true;
        }

        public void Update()
        {
            changed = true;
        }

        private void SaveConfig()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (changed == true)
                    {
                        changed = false;
                        configDataProvider.Save(config).Wait();
                    }
                    await Task.Delay(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }
    }

    [Table("sign")]
    public sealed class SignCacheFileInfo
    {
        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();
    }

    public sealed class SignCacheInfo
    {
        public string MachineName { get; set; }
        public string Version { get; set; } = "1.0.0.0";

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        public bool Connected
        {
            get
            {
                return Connection != null && Connection.Connected == true;
            }
        }
        [JsonIgnore]
        public IConnection Connection { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SignInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
