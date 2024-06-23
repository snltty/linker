using LiteDB;
using System.Net;
using System.Text.Json.Serialization;

namespace link.client.config
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

        public IPAddress BindIPAddress { get; set; } = IPAddress.Any;
        public int Port { get; set; }
        public IPEndPoint TargetEP { get; set; }
        public bool Started { get; set; }

        [JsonIgnore,BsonIgnore]
        public bool Proxy { get; set; }

    }
}
