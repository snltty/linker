using linker.libs;
using linker.libs.extends;
using LiteDB;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using linker.messenger.serializer.aot;
using linker.libs.timer;

namespace linker.messenger.store.file
{
    public sealed class FileConfig
    {
        private SemaphoreSlim slim = new SemaphoreSlim(1);
        private string configPath = "./configs/";

        private Dictionary<string, FileReadWrite> fsDic = new Dictionary<string, FileReadWrite>();

        public ConfigInfo Data { get; private set; } = new ConfigInfo();

        public FileConfig()
        {
        }
        public void Initialize(Dictionary<string, string> dic)
        {
            Init();
            Load(dic);
            Save();
            SaveTask();
        }

        private void Init()
        {
            if (Directory.Exists(Path.Combine(Helper.currentDirectory, configPath)) == false)
            {
                Directory.CreateDirectory(Path.Combine(Helper.currentDirectory, configPath));
            }

            Type type = Data.GetType();
            Type typeAttr = typeof(JsonIgnoreAttribute);
            foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.GetCustomAttribute(typeAttr) == null))
            {
                object property = item.GetValue(Data);
                fsDic.Add(item.Name.ToLower(), new FileReadWrite
                {
                    Path = Path.Combine(Helper.currentDirectory,configPath, $"{item.Name.ToLower()}.json"),
                    Property = item,
                    PropertyObject = property,
                    PropertyMethod = (IConfig)property,
                });
            }
        }
        private void Load(Dictionary<string, string> dic)
        {
            slim.Wait();
            try
            {
                foreach (var item in fsDic)
                {
                    try
                    {
                        if (item.Value.PropertyObject == null)
                        {
                            LoggerHelper.Instance.Error($"{item.Value.Property.Name} not found");
                            continue;
                        }
                        string text = string.Empty;
                        if (File.Exists(item.Value.Path))
                        {
                            text = File.ReadAllText(item.Value.Path, encoding: System.Text.Encoding.UTF8);
                        }
                        else if (dic != null && dic.TryGetValue(item.Value.Property.Name, out string base64))
                        {
                            text = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        }
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            LoggerHelper.Instance.Error($"{item.Value.Path} empty");
                            continue;
                        }
                        object value = item.Value.PropertyMethod.Deserialize(text);
                        item.Value.Property.SetValue(Data, value);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
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
                    try
                    {
                        if (item.Value.PropertyObject == null)
                        {
                            LoggerHelper.Instance.Error($"{item.Value.Property.Name} save not found");
                            continue;
                        }
                        string text = item.Value.PropertyMethod.Serialize(item.Value.Property.GetValue(Data));
                        File.WriteAllText($"{item.Value.Path}.temp", text, encoding: System.Text.Encoding.UTF8);
                        File.Move($"{item.Value.Path}.temp", item.Value.Path, true);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
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
            TimerHelper.SetIntervalLong(() =>
            {
                while (Data.Updated > 0)
                {
                    Save();
                    Data.Updated--;
                }
            }, 3000);
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
        public ConfigInfo() { }
        public ConfigCommonInfo Common { get; set; } = new ConfigCommonInfo();
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();

        [JsonIgnore, BsonIgnore]
        public uint Updated { get; set; } = 1;

        public void Update()
        {
            Updated++;
        }
    }

    [JsonAot]
    public sealed partial class ConfigClientInfo : IConfig
    {
        private ICrypto crypto;
        public ConfigClientInfo()
        {
            crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);
        }

        public string Serialize(object obj)
        {
#if DEBUG
            return obj.ToJsonFormat();
#else
            return Convert.ToBase64String(crypto.Encode(Encoding.UTF8.GetBytes(obj.ToJson())));
#endif
        }
        public object Deserialize(string text)
        {
            if (text.Contains("ApiPassword"))
            {
                return text.DeJson<ConfigClientInfo>();
            }
            return Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(text)).ToArray()).DeJson<ConfigClientInfo>();
        }
    }

    [JsonAot]
    public sealed partial class ConfigServerInfo : IConfig
    {
        public ConfigServerInfo() { }
        public object Deserialize(string text)
        {
            return text.DeJson<ConfigServerInfo>();
        }
        public string Serialize(object obj)
        {
            return obj.ToJsonFormat();
        }
    }

    [JsonAot]
    public sealed partial class ConfigCommonInfo : IConfig
    {
        public ConfigCommonInfo() { }


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
