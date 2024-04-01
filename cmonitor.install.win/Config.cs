using common.libs;
using common.libs.extends;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.install.win
{
    public sealed class Config
    {
        public Config()
        {
            Load();
        }

        public ConfigCommonInfo Common { get; set; } = new ConfigCommonInfo();
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();

        public string ConfigPath { get; } = "./configs/";


        private Dictionary<string, Dictionary<string, object>> JsonDic = new Dictionary<string, Dictionary<string, object>>();
        public T Get<T>(T defaultValue)
        {
            return Get<T>(typeof(T).Name, defaultValue);
        }
        public T Get<T>(string name, T defaultValue)
        {
            if (JsonDic.ContainsKey(name))
            {
                return JsonDic[name].ToJson().DeJson<T>();
            }
            return defaultValue;
        }
        public void Set<T>(T value)
        {
            Set<T>(typeof(T).Name, value);
        }
        public void Set<T>(string name, T value)
        {
            JsonDic[name] = value.ToJson().DeJson<Dictionary<string, object>>();
            Save();
        }

        private void Load()
        {
            InitFileConfig();
            ReadJson();
            Save();
        }
        private void InitFileConfig()
        {
            try
            {
                if (Directory.Exists(ConfigPath) == false)
                {
                    Directory.CreateDirectory(ConfigPath);
                }
                string path = Path.Join(ConfigPath, "config.json");
                if (File.Exists(path) == false)
                {
                    return;
                }

                string text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                JsonDic = text.DeJson<Dictionary<string, Dictionary<string, object>>>();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private void ReadJson()
        {
            if (JsonDic.TryGetValue("Client", out Dictionary<string, object> elClient))
            {
                Client = elClient.ToJson().DeJson<ConfigClientInfo>();
            }

            if (JsonDic.TryGetValue("Server", out Dictionary<string, object> elServer))
            {
                Server = elServer.ToJson().DeJson<ConfigServerInfo>();
            }
            if (JsonDic.TryGetValue("Common", out Dictionary<string, object> elCommon))
            {
                Common = elCommon.ToJson().DeJson<ConfigCommonInfo>();
            }
        }

        public void Save()
        {
            try
            {
                if (Directory.Exists(ConfigPath) == false)
                {
                    Directory.CreateDirectory(ConfigPath);
                }
                string path = Path.Join(ConfigPath, "config.json");

                JsonDic["Client"] = Client.ToJson().DeJson<Dictionary<string, object>>();
                JsonDic["Server"] = Server.ToJson().DeJson<Dictionary<string, object>>();
                JsonDic["Common"] = Common.ToJson().DeJson<Dictionary<string, object>>();
                File.WriteAllText(path, JsonDic.ToJsonFormat());
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }

    public sealed class ConfigCommonInfo
    {
        public string[] Modes { get; set; } = new string[] { "client", "server" };
    }
    public sealed class ConfigClientInfo
    {
        private string server = new IPEndPoint(IPAddress.Loopback, 1802).ToString();
        public string Server
        {
            get => server; set
            {
                server = value;
                if (string.IsNullOrWhiteSpace(server) == false)
                {
                    string[] arr = server.Split(':');
                    int port = arr.Length == 2 ? int.Parse(arr[1]) : 1802;
                    IPAddress ip = NetworkHelper.GetDomainIp(arr[0]);
                    ServerEP = new IPEndPoint(ip, port);
                }
            }
        }

        [JsonIgnore]
        public IPEndPoint ServerEP { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802);

        private string name = Dns.GetHostName().SubStr(0, 12);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 12);
            }
        }
        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryCount { get; set; } = 100;
        public int ShareMemorySize { get; set; } = 1024;

        public bool BlueProtect { get; set; } = false;

    }
    public sealed class ConfigServerInfo
    {
        public int WebPort { get; set; } = 1800;
        public string WebRoot { get; set; } = "./web/";
        public int ApiPort { get; set; } = 1801;
        public int ServicePort { get; set; } = 1802;

    }
}
