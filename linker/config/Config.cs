using linker.libs;
using linker.libs.extends;
using LiteDB;
using System.Reflection;
using System.Text.Json.Serialization;

namespace linker.config
{
    public sealed class FileConfig
    {
        private SemaphoreSlim slim = new SemaphoreSlim(1);
        private string configPath = "./configs/";

        private Dictionary<string, FileReadWrite> fsDic = new Dictionary<string, FileReadWrite>();

        public ConfigInfo Data { get; private set; } = new ConfigInfo();

        public FileConfig()
        {
            Init();
            Load();
            Save();
            SaveTask();
        }

        private void Init()
        {
            if (Directory.Exists(configPath) == false)
            {
                Directory.CreateDirectory(configPath);
            }

            Type type = Data.GetType();
            Type typeAttr = typeof(JsonIgnoreAttribute);
            foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.GetCustomAttribute(typeAttr) == null))
            {

                object property = item.GetValue(Data);
                fsDic.Add(item.Name.ToLower(), new FileReadWrite
                {
                    Path = Path.Join(configPath, $"{item.Name.ToLower()}.json"),
                    Property = item,
                    PropertyObject = property,
                    PropertyMethod = (IConfig)property,
                });
            }
        }
        private void Load()
        {
            slim.Wait();
            try
            {
                foreach (var item in fsDic)
                {
                    if (item.Value.PropertyObject == null)
                    {
                        continue;
                    }
                    if (File.Exists(item.Value.Path) == false) continue;

                    string text = File.ReadAllText(item.Value.Path);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }
                    object value = item.Value.PropertyMethod.Deserialize(text);
                    item.Value.Property.SetValue(Data, value);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }

        }
        public void Save()
        {
            slim.Wait();
            try
            {
                foreach (var item in fsDic)
                {
                    if (item.Value.PropertyObject == null)
                    {
                        continue;
                    }
                    string text = item.Value.PropertyMethod.Serialize(item.Value.Property.GetValue(Data));
                    File.WriteAllText(item.Value.Path, text);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }

        private void SaveTask()
        {
            TimerHelper.SetInterval(() =>
            {
                while (Data.Updated > 0)
                {
                    Save();
                    Data.Updated--;
                }
                return true;
            }, 1000);
        }
    }

    public sealed class FileReadWrite
    {
        public string Path { get; set; }

        public PropertyInfo Property { get; set; }
        public object PropertyObject { get; set; }
        public IConfig PropertyMethod { get; set; }
    }


    public interface IConfig
    {
        public string Serialize(object obj);
        public object Deserialize(string text);
    }
    public sealed partial class ConfigInfo
    {
        public ConfigCommonInfo Common { get; set; } = new ConfigCommonInfo();

        [JsonIgnore]
        public string Version { get; set; } = $"v{string.Join(".", Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3))}";

        [JsonIgnore]
        public bool Elevated { get; set; }


        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public void Update()
        {
            Updated++;
        }
    }

    public sealed partial class ConfigCommonInfo : IConfig
    {
        public string[] Modes { get; set; } = new string[] { "client", "server" };



#if DEBUG
        private LoggerTypes loggerType { get; set; } = LoggerTypes.DEBUG;
        public bool Install { get; set; } = false;
#else
        private LoggerTypes loggerType { get; set; } = LoggerTypes.WARNING;
        public bool Install { get; set; } = false;
#endif

        [JsonIgnore]
        public string[] Plugins { get; set; } = Array.Empty<string>();

        public LoggerTypes LoggerType
        {
            get => loggerType; set
            {
                loggerType = value;
                LoggerHelper.Instance.LoggerLevel = value;
            }
        }
        public int LoggerSize { get; set; } = 100;

        [JsonIgnore]
        public string[] IncludePlugins { get; set; } = Array.Empty<string>();
        [JsonIgnore]
        public string[] ExcludePlugins { get; set; } = Array.Empty<string>();


        public ConfigCommonInfo Load(string text)
        {
            return text.DeJson<ConfigCommonInfo>();
        }

        public string Serialize(object obj)
        {
            return obj.ToJsonFormat();
        }

        public object Deserialize(string text)
        {
            return text.DeJson<ConfigCommonInfo>();
        }
    }
}
