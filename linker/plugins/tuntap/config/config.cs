using linker.plugins.tuntap.config;
using MemoryPack;
using System.Buffers.Binary;
using System.Net;

namespace linker.plugins.tuntap.config
{
    public sealed class TuntapConfigInfo
    {
        private IPAddress ip = IPAddress.Any;
        /// <summary>
        /// 网卡IP
        /// </summary>
        public IPAddress IP
        {
            get => ip; set
            {
                ip = value;
                IpInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            }
        }
        public uint IpInt { get; private set; }
        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 局域网掩码列表，与IP列表一一对应
        /// </summary>
        public int[] Masks { get; set; } = Array.Empty<int>();
        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool Running { get; set; }

        public bool Gateway { get; set; }
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

        [MemoryPackIgnore]
        public IPAddress OriginIPAddress { get; set; }


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
        Operating = 1,
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
        public int[] Masks { get; set; } = Array.Empty<int>();

        public string Error { get; set; }
        public string System { get; set; }

        public bool Gateway { get; set; }
    }

}


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 虚拟网卡配置
        /// </summary>
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}