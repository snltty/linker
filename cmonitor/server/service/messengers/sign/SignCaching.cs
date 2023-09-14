using common.libs.database;
using MemoryPack;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace cmonitor.server.service.messengers.sign
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
            cache = new SignCacheInfo
            {
                Connection = connection,
                MachineName = signInfo.MachineName,
                Version = signInfo.Version
            };
            config.Clients.TryAdd(signInfo.MachineName, cache);
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

        private void SaveConfig()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (changed == true)
                    {
                        changed = false;
                        configDataProvider.Save(config).Wait();
                    }
                    Thread.Sleep(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }
    }

    [Table("sign-cache")]
    public sealed class SignCacheFileInfo
    {
        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();
    }

    public sealed class SignCacheInfo
    {
        public string MachineName { get; set; }
        public string Version { get; set; } = "1.0.0.0";

        [JsonIgnore]
        public int ReportFlag = 1;
        [JsonIgnore]
        public int ReportTime = Environment.TickCount;
        [JsonIgnore]
        public int PingFlag = 1;
        [JsonIgnore]
        public int ScreenFlag = 1;
        [JsonIgnore]
        public int ScreenTime = Environment.TickCount;

        public bool GetReport()
        {
            return Environment.TickCount - ReportTime > Config.ReportTime;
        }
        public void UpdateReport()
        {
             ReportTime = Environment.TickCount;
        }

        public bool GetScreen()
        {
            return Environment.TickCount - ScreenTime > Config.ScreenTime;
        }
        public void UpdateScreen()
        {
            ScreenTime = Environment.TickCount;
        }


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
        public string MachineName { get; set; }
        public string Version { get; set; }
    }
}
