using linker.libs.extends;

namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigActionInfo Action { get; set; } = new ConfigActionInfo();
    }
    public sealed partial class ConfigActionInfo : IConfig
    {
        public string SignInActionUrl { get; set; } = string.Empty;
        public string RelayActionUrl { get; set; } = string.Empty;
        public string SForwardActionUrl { get; set; } = string.Empty;

        public object Deserialize(string text)
        {
            return text.DeJson<ConfigActionInfo>();
        }
        public string Serialize(object obj)
        {
            return obj.ToJsonFormat();
        }
    }

    public sealed partial class ConfigClientInfo
    {
        public ConfigClientActionInfo Action { get; set; } = new ConfigClientActionInfo();
    }

    public sealed partial class ConfigClientActionInfo
    {
        public string Arg { get; set; }
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
