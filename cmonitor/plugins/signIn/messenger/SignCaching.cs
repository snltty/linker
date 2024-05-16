using cmonitor.server;
using common.libs.database;
using MemoryPack;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
            foreach (var item in config.Clients.Values)
            {
                item.Connected = false;
            }
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
                GroupId = signInfo.GroupId,
            };
            config.Clients.TryAdd(signInfo.MachineName, cache1);
            changed = true;
        }
        public bool Get(string machineName, out SignCacheInfo cache)
        {
            return config.Clients.TryGetValue(machineName, out cache);
        }
        public List<SignCacheInfo> Get(string groupId)
        {
            return config.Clients.Values.Where(c => c.GroupId == groupId).ToList();
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
            Task.Run(async () =>
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

            });
        }
    }

    [Table("sign")]
    public sealed class SignCacheFileInfo
    {
        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();
    }

    [MemoryPackable]
    public sealed partial class SignCacheInfo
    {
        public string MachineName { get; set; }
        public string Version { get; set; } = "1.0.0.0";
        public string GroupId { get; set; } = "snltty";
        public DateTime LastSignIn { get; set; } = DateTime.Now;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();


        private IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        [MemoryPackAllowSerialize]
        public IPEndPoint IP
        {
            get
            {
                if (Connection != null)
                {
                    ip = Connection.Address;
                }
                return ip;
            }
            set
            {
                ip = value;
            }
        }

        private bool connected = false;
        public bool Connected
        {
            get
            {
                if (Connection != null)
                {
                    connected =  Connection.Connected == true;
                }
                return connected;
            }
            set
            {
                connected = value;
            }
        }

        [JsonIgnore]
        [MemoryPackIgnore]
        public IConnection Connection { get; set; }
    }


    [MemoryPackable]
    public sealed partial class SignInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
