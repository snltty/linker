using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using LiteDB;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{
    public sealed class FileConfigInitParams
    {
        public bool UseMemory { get; set; } = false;
        public Dictionary<string, string> InitialData { get; set; } = [];
    }
    public sealed class FileConfig
    {
        private readonly SemaphoreSlim slim = new(1);
        private readonly string configPath = "./configs/";

        private readonly Dictionary<string, FileReadWrite> fsDic = [];
        private List<string[]> saveJsonIgnorePaths = [];

        public static bool ForceInMemory { get; set; } = false;
        private readonly bool useMemory = false;
        private readonly Dictionary<string, string> memoryStore = [];

        public ConfigInfo Data { get; private set; } = new ConfigInfo();

        public FileConfig(FileConfigInitParams initParams = null)
        {
            if (initParams != null)
            {
                useMemory = initParams.UseMemory;
                if (initParams.InitialData != null)
                {
                    foreach (var kv in initParams.InitialData)
                    {
                        memoryStore[kv.Key.ToLower()] = kv.Value;
                    }
                }
            }
            else
            {
                useMemory = ForceInMemory;
            }

            Init();
            Load();
            Save();
            SaveTask();
        }
        private void Init()
        {
            if (Directory.Exists(Path.Combine(Helper.CurrentDirectory, configPath)) == false && useMemory == false)
            {
                Directory.CreateDirectory(Path.Combine(Helper.CurrentDirectory, configPath));
            }

            Type saveAttr = typeof(SaveJsonIgnore);
            saveJsonIgnorePaths = FindPathsWithSaveJsonIgnore(Data);
            List<string[]> FindPathsWithSaveJsonIgnore(object obj)
            {
                var paths = new List<string[]>();
                FindPathsRecursive(obj, string.Empty, paths);
                return paths;
            }
            void FindPathsRecursive(object obj, string currentPath, List<string[]> resultPaths)
            {
                if (obj == null) return;

                Type type = obj.GetType();
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    string propertyPath = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";
                    if (Attribute.IsDefined(property, saveAttr))
                    {
                        resultPaths.Add([.. propertyPath.Split('.')]);
                    }
                    else if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && !property.PropertyType.IsArray && !typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        try
                        {
                            object value = property.GetValue(obj);
                            FindPathsRecursive(value, propertyPath, resultPaths);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    }
                }
            }


            Type type = Data.GetType();
            Type typeAttr = typeof(JsonIgnoreAttribute);
            foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.GetCustomAttribute(typeAttr) == null))
            {
                object property = item.GetValue(Data);
                fsDic.Add(item.Name.ToLower(), new FileReadWrite
                {
                    Path = Path.Combine(Helper.CurrentDirectory, configPath, $"{item.Name.ToLower()}.json"),
                    Property = item,
                    PropertyObject = property,
                    PropertyMethod = (IConfig)property,
                });
            }
        }

        private string ToText(KeyValuePair<string, FileReadWrite> item)
        {
            if (useMemory)
            {
                if (memoryStore.TryGetValue(item.Key.ToLower(), out var memText))
                {
                    return memText;
                }
            }
            else
            {
                if (File.Exists(item.Value.Path))
                {
                    return File.ReadAllText(item.Value.Path, encoding: System.Text.Encoding.UTF8);
                }
            }
            return string.Empty;
        }
        private void Load()
        {
            slim.Wait();
            try
            {
                foreach (KeyValuePair<string, FileReadWrite> item in fsDic)
                {
                    try
                    {
                        if (item.Value.PropertyObject == null)
                        {
                            continue;
                        }

                        string text = ToText(item);
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

        private void IgnorePaths(KeyValuePair<string, FileReadWrite> item, JsonNode jsonNode)
        {
            try
            {
                foreach (string[] path in saveJsonIgnorePaths.Where(c => c.Length > 0 && c[0] == item.Value.Property.Name))
                {
                    if (path.Length == 0) continue;

                    JsonNode root = jsonNode;
                    for (int i = 1; i < path.Length - 1; i++)
                    {
                        if (root.AsObject().TryGetPropertyValue(path[i], out root) == false || root == null) break;
                        if (root == null) break;
                    }
                    root?.AsObject().Remove(path[^1]);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private string ToText(JsonDocument json, KeyValuePair<string, FileReadWrite> item, JsonNode jsonNode)
        {
            string text = item.Value.PropertyMethod.Serialize(jsonNode);
            if (json != null && json.RootElement.TryGetProperty(item.Value.Property.Name, out JsonElement import))
            {
                text = item.Value.PropertyMethod.Deserialize(text).ToJson();
                text = MergeJson(text, import.ToJson());

                object value = item.Value.PropertyMethod.Deserialize(text);
                text = item.Value.PropertyMethod.Serialize(value);

                item.Value.Property.SetValue(Data, value);
            }
            return text;

        }
        public void Save(JsonDocument json = null)
        {
            slim.Wait();
            try
            {
                foreach (KeyValuePair<string, FileReadWrite> item in fsDic)
                {
                    string tempFilePath = $"{item.Value.Path}.temp";
                    try
                    {
                        if (item.Value.PropertyObject == null)
                        {
                            continue;
                        }

                        JsonNode jsonNode = JsonNode.Parse(item.Value.Property.GetValue(Data).ToJson());
                        IgnorePaths(item, jsonNode);
                        string text = ToText(json, item, jsonNode);

                        if (useMemory)
                        {
                            memoryStore[item.Key.ToLower()] = text;
                        }
                        else
                        {
                            File.WriteAllText(tempFilePath, text, encoding: System.Text.Encoding.UTF8);
                            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite))
                            {
                                fileStream.Flush(true);
                                fileStream.Dispose();
                            }

                            File.Move(tempFilePath, item.Value.Path, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
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
                GC.Collect();
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

        public static string MergeJson(string json1, string json2)
        {
            using var doc1 = JsonDocument.Parse(json1);
            using var doc2 = JsonDocument.Parse(json2);

            var output = new Dictionary<string, object>();

            foreach (var property in doc1.RootElement.EnumerateObject())
            {
                output[property.Name] = GetValue(property.Value);
            }
            foreach (var property in doc2.RootElement.EnumerateObject())
            {
                output[property.Name] = GetValue(property.Value);
            }

            return output.ToJson();

            object GetValue(JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Object => GetObjectValue(element),
                    JsonValueKind.Array => GetArrayValue(element),
                    JsonValueKind.Undefined => null,
                    _ => null
                };
            }

            Dictionary<string, object> GetObjectValue(JsonElement element)
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = GetValue(prop.Value);
                }
                return dict;
            }

            List<object> GetArrayValue(JsonElement element)
            {
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(GetValue(item));
                }
                return list;
            }
        }
        public IReadOnlyDictionary<string, string> GetMemoryStore() => new Dictionary<string, string>(memoryStore);
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
        [JsonIgnore, BsonIgnore]
        public VersionManager DataVersion { get; set; } = new VersionManager();

        public void Update()
        {
            Updated++;
            DataVersion.Increment();
        }
    }

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
            try
            {
                return text.DeJson<ConfigClientInfo>();
            }
            catch (Exception)
            {
            }
            return Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(text)).Span).DeJson<ConfigClientInfo>();
        }
    }
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

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SaveJsonIgnore : Attribute
    {
    }

}
