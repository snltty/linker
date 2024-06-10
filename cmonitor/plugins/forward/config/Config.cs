using cmonitor.plugins.forward.proxy;
using LiteDB;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public List<ForwardInfo> Forwards { get; set; }= new List<ForwardInfo>();
    }

    public sealed class ForwardInfo
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string MachineId { get; set; }
        public int Port { get; set; }
        public IPEndPoint TargetEP { get; set; }
        public bool Started { get; set; }

        [JsonIgnore,BsonIgnore]
        public ForwardProxy Proxy { get; set; }

    }
}
