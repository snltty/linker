using common.libs;
using common.libs.extends;
using System.Text.Json.Serialization;

namespace cmonitor.config
{
    public sealed class Config
    {
        private FileStream fs = null;
        private StreamWriter writer = null;
        private StreamReader reader = null;
        private SemaphoreSlim slim = new SemaphoreSlim(1);
        private string configPath = "./configs/";

        public ConfigInfo Data { get; private set; } = new ConfigInfo();

        public Config()
        {
            if (Directory.Exists(configPath) == false)
            {
                Directory.CreateDirectory(configPath);
            }
            string path = Path.Join(configPath, "config.json");
            fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            reader = new StreamReader(fs, System.Text.Encoding.UTF8);
            writer = new StreamWriter(fs, System.Text.Encoding.UTF8);

            Load();
            Save();
        }

        private void Load()
        {
            slim.Wait();
            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                string text = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                Data = text.DeJson<ConfigInfo>();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
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
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);
                writer.Write(Data.ToJsonFormat());
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            finally
            {
                slim.Release();
            }
        }


    }

    public sealed partial class ConfigInfo
    {
        public ConfigCommonInfo Common { get; set; } = new ConfigCommonInfo();

        [JsonIgnore]
        public string Version { get; set; } = "1.0.0.1";
        [JsonIgnore]
        public bool Elevated { get; set; }
    }

    public sealed partial class ConfigCommonInfo
    {
        public string[] Modes { get; set; } = new string[] { "client", "server" };

        private string[] includePlugins = Array.Empty<string>();
        public string[] IncludePlugins
        {
            get => includePlugins; set
            {
                includePlugins = value;
                if (includePlugins.Length > 0)
                {
                    includePlugins = includePlugins.Concat(new List<string>
                    {
                        "cmonitor.client.","cmonitor.server.","cmonitor.serializes.",
                        "cmonitor.plugins.signin.", "cmonitor.plugins.watch.","cmonitor.plugins.devices.","cmonitor.plugins.report.",
                        "cmonitor.plugins.share.","cmonitor.plugins.rule.","cmonitor.plugins.modes.","cmonitor.plugins.tunnel.","cmonitor.plugins.relay",
                    }).Distinct().ToArray();
                }
            }
        }
        public string[] ExcludePlugins { get; set; } = Array.Empty<string>();

        public IEnumerable<Type> PluginContains(IEnumerable<Type> types)
        {
            if (IncludePlugins.Length > 0)
            {
                types = types.Where(c => IncludePlugins.Any(d => c.FullName.Contains(d)));
            }
            if(ExcludePlugins.Length > 0)
            {
                types = types.Where(c => ExcludePlugins.Any(d => c.FullName.Contains(d) == false));
            }
            return types;
        }
    }
}
