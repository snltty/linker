using MemoryPack;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace linker.plugins.tuntap.vea
{
    public interface ITuntapVea
    {
        public bool Running { get; }
        public string InterfaceName { get; }
        public string Error { get; }

        public Task<bool> Run(int proxyPort,IPAddress ip);
        public Task<bool> SetIp(IPAddress ip);
        public void Kill();

        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip);
        public void DelRoute(TuntapVeaLanIPAddress[] ips);


    }

    [MemoryPackable]
    public sealed partial class TuntapVeaLanIPAddress
    {
        /// <summary>
        /// ip，存小端
        /// </summary>
        public uint IPAddress { get; set; }
        public byte MaskLength { get; set; }
        public uint MaskValue { get; set; }
        public uint NetWork { get; set; }
        public uint Broadcast { get; set; }

    }

    [MemoryPackable]
    public sealed partial class TuntapVeaLanIPAddressList
    {
        public string MachineId { get; set; }
        public List<TuntapVeaLanIPAddress> IPS { get; set; }

    }

    public enum TuntapStatus : byte
    {
        Normal = 0,
        Starting = 1,
        Running = 2
    }

    [MemoryPackable]
    public sealed partial class TuntapInfo
    {
        public string MachineId { get; set; }

        public TuntapStatus Status { get; set; }
       
        [MemoryPackAllowSerialize]
        public IPAddress IP { get; set; }

        [MemoryPackAllowSerialize]
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();

        public string Error { get; set; }

    }

    [MemoryPackable]
    public sealed partial class TuntapOnlineInfo
    {
        public string[] MachineIds { get; set; }
        public byte[] Status { get; set; }

        [JsonIgnore]
        [MemoryPackInclude]
        public List<Task<IPHostEntry>> HostTasks { get; set; }
        [JsonIgnore]
        [MemoryPackInclude]
        public List<Task<PingReply>> PingTasks { get; set; }
    }
}

