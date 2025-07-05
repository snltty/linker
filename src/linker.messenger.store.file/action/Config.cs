using linker.libs.extends;
using linker.messenger.action;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigInfo
    {
        public ConfigActionInfo Action { get; set; } = new ConfigActionInfo();
    }
    public sealed partial class ConfigActionInfo : IConfig
    {
        public ConfigActionInfo() { }
        public string SignInActionUrl { get; set; } = string.Empty;
        public string RelayActionUrl { get; set; } = string.Empty;
        public string RelayNodeUrl { get; set; } = string.Empty;
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
        public ActionInfo Action { get; set; } = new ActionInfo();
    }
}
