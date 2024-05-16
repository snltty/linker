using cmonitor.plugins.forward.proxy;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.config
{
    public partial class ConfigClientInfo
    {
        public ForwardConfigClientInfo Forward { get; set; } = new ForwardConfigClientInfo();
    }
    public sealed class ForwardConfigClientInfo
    {
        public List<ForwardInfo> Forwards { get; set; }= new List<ForwardInfo>();
    }

    public sealed class ForwardInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public string MachineName { get; set; }
        public int Port { get; set; }
        public IPEndPoint TargetEP { get; set; }
        public bool Started { get; set; }

        [JsonIgnore]
        public ForwardProxy Proxy { get; set; }

    }
}
