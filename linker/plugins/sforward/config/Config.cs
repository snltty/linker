using linker.libs;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public string SForwardSecretKey { get; set; } = "snltty";
        public List<SForwardInfo> SForwards { get; set; } =new List<SForwardInfo>();
    }

    public sealed class SForwardInfo
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public string Domain { get; set; }
        public int RemotePort { get; set; }

        public IPEndPoint LocalEP { get; set; }

        public bool Started { get; set; }

        [JsonIgnore, BsonIgnore]
        public bool Proxy { get; set; }
    }

}

namespace linker.config
{
    public partial class ConfigServerInfo
    {
        public SForwardConfigServerInfo SForward { get; set; } = new SForwardConfigServerInfo();
    }

    public sealed class SForwardConfigServerInfo
    {
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();

        public int WebPort { get; set; }
        public int[] TunnelPortRange { get; set; } = new int[] { 10000, 60000 };

    }

}

namespace linker.plugins.sforward.config
{
    [MemoryPackable]
    public sealed partial class SForwardAddInfo
    {
        public string Domain { get; set; }
        public int RemotePort { get; set; }
        public string SecretKey { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SForwardAddResultInfo
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }


    [MemoryPackable]
    public sealed partial class SForwardProxyInfo
    {
        public ulong Id { get; set; }
        public string Domain { get; set; }
        public int RemotePort { get; set; }
    }
}
