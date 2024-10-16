using linker.libs.extends;
namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();
    }
    public sealed partial class ConfigServerInfo : IConfig
    {
        public ConfigServerInfo() { }
        public int ServicePort { get; set; } = 1802;

        public ServerCertificateInfo SSL { get; set; } = new ServerCertificateInfo();

        public object Deserialize(string text)
        {
            return text.DeJson<ConfigServerInfo>();
        }
        public string Serialize(object obj)
        {
            return obj.ToJsonFormat();
        }
    }

    public sealed partial class ServerCertificateInfo
    {
        public ServerCertificateInfo() { }
        public string File { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";
    }
}
