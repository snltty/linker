using linker.libs.extends;
using linker.plugins.messenger;
using linker.plugins.resolver;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;

namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigServerInfo Server { get; set; } = new ConfigServerInfo();
    }
    public sealed partial class ConfigServerInfo : IConfig
    {
        public int ServicePort { get; set; } = 1802;

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";

        public object Deserialize(string text)
        {
            return text.DeJson<ConfigServerInfo>();
        }
        public string Serialize(object obj)
        {
            return obj.ToJsonFormat();
        }
    }


    [MemoryPackable]
    public sealed partial class ServerFlowInfo
    {
        public Dictionary<string, ResolverFlowItemInfo> Resolvers { get; set; }
        public Dictionary<ushort, MessengerFlowItemInfo> Messangers { get; set; }

        public DateTime Start { get; set; }
        public DateTime Now { get; set; }
    }


}
