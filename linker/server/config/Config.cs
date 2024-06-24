using Linker.Libs;
using Linker.Libs.Extends;

namespace Linker.Config
{
    public sealed partial class ConfigInfo
    {
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();
    }
    public sealed partial class ConfigServerInfo
    {
        public int ServicePort { get; set; } = 1802;

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";

        public ConfigServerInfo Load(string text)
        {
            return text.DeJson<ConfigServerInfo>();
        }
    }
}
