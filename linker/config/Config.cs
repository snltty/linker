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
                FileStream fs = new FileStream(Path.Join(configPath, $"{item.Name.ToLower()}.json"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                object property = item.GetValue(Data);
                MethodInfo method = property.GetType().GetMethod("Load");
                fsDic.Add(item.Name.ToLower(), new FileReadWrite
                {
                    FS = fs,
                    SR = new StreamReader(fs, System.Text.Encoding.UTF8),
                    SW = new StreamWriter(fs, System.Text.Encoding.UTF8),
                    Property = item,
                    PropertyObject = property,
                    PropertyLoadMethod = method
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
                    if (item.Value.PropertyObject == null || item.Value.PropertyLoadMethod == null)
                    {
                        continue;
                    }
                    item.Value.FS.Seek(0, SeekOrigin.Begin);
                    string text = item.Value.SR.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }
                    object value = item.Value.PropertyLoadMethod.Invoke(item.Value.PropertyObject, new object[] { text });
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
        private void Save()
        {
            slim.Wait();
            try
            {
                foreach (var item in fsDic)
                {
                    if (item.Value.PropertyObject == null || item.Value.PropertyLoadMethod == null)
                    {
                        continue;
                    }
                    item.Value.FS.Seek(0, SeekOrigin.Begin);
                    item.Value.FS.SetLength(0);
                    item.Value.SW.Write(item.Value.Property.GetValue(Data).ToJsonFormat());
                    item.Value.SW.Flush();
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
            Task.Run(async () =>
            {
                while (true)
                {
                    while (Data.Updated > 0)
                    {
                        Save();
                        Data.Updated--;
                    }
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            });
        }
    }

    public sealed class FileReadWrite
    {
        public FileStream FS { get; set; }
        public StreamReader SR { get; set; }
        public StreamWriter SW { get; set; }

        public PropertyInfo Property { get; set; }
        public object PropertyObject { get; set; }
        public MethodInfo PropertyLoadMethod { get; set; }
    }


    public sealed partial class ConfigInfo
    {
        public ConfigCommonInfo Common { get; set; } = new ConfigCommonInfo();

        [JsonIgnore]
        public string Version { get; set; } = $"v{Assembly.GetEntryAssembly().GetName().Version}";
        [JsonIgnore]
        public bool Elevated { get; set; }


        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public void Update()
        {
            Updated++;
        }
    }

    public sealed partial class ConfigCommonInfo
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

        public string[] IncludePlugins { get; set; } = Array.Empty<string>();
        public string[] ExcludePlugins { get; set; } = Array.Empty<string>();


        public ConfigCommonInfo Load(string text)
        {
            return text.DeJson<ConfigCommonInfo>();
        }
    }
}
