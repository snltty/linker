using linker.plugins.tuntap.config;
using MemoryPack;
using System.Net;

namespace linker.plugins.tuntap.config
{
    public sealed class TuntapConfigInfo
    {
        /// <summary>
        /// 网卡IP
        /// </summary>
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 局域网掩码列表，与IP列表一一对应
        /// </summary>
        public int[] Masks { get; set; } = Array.Empty<int>();

        /// <summary>
        /// 前缀长度
        /// </summary>
        public byte PrefixLength { get; set; } = 24;

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool Running { get; set; }
        /// <summary>
        /// 是否网关
        /// </summary>
        public bool Gateway { get; set; }
        /// <summary>
        /// 使用高级功能
        /// </summary>
        public bool Upgrade { get; set; }
        /// <summary>
        /// 端口转发列表
        /// </summary>
        public List<TuntapForwardInfo> Forwards { get; set; } = new List<TuntapForwardInfo>();
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
        /// <summary>
        /// 无
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 操作中
        /// </summary>
        Operating = 1,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 2
    }

    [MemoryPackable]
    public sealed partial class TuntapInfo
    {
        /// <summary>
        /// 设备id
        /// </summary>
        public string MachineId { get; set; }

        /// <summary>
        /// 虚拟网卡状态
        /// </summary>
        public TuntapStatus Status { get; set; }
        /// <summary>
        /// 虚拟网卡IP
        /// </summary>

        [MemoryPackAllowSerialize]
        public IPAddress IP { get; set; }
        /// <summary>
        /// 局域网IP
        /// </summary>

        [MemoryPackAllowSerialize]
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 局域网IP掩码
        /// </summary>
        public int[] Masks { get; set; } = Array.Empty<int>();


        /// <summary>
        /// 前缀长度
        /// </summary>
        public byte PrefixLength { get; set; } = 24;

        /// <summary>
        /// 网卡安装错误
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// NAT设置错误
        /// </summary>
        public string Error1 { get; set; }
        /// <summary>
        /// 系统信息
        /// </summary>
        public string SystemInfo { get; set; }
        /// <summary>
        /// 是否网关
        /// </summary>
        public bool Gateway { get; set; }
        /// <summary>
        /// 使用高级功能
        /// </summary>
        public bool Upgrade { get; set; }
        /// <summary>
        /// 端口转发列表
        /// </summary>
        public List<TuntapForwardInfo> Forwards { get; set; } = new List<TuntapForwardInfo>();
    }


    [MemoryPackable]
    public sealed partial class TuntapForwardInfo
    {
        [MemoryPackAllowSerialize]
        public IPAddress ListenAddr { get; set; } = IPAddress.Any;
        public int ListenPort { get; set; }

        [MemoryPackAllowSerialize]
        public IPAddress ConnectAddr { get; set; } = IPAddress.Any;
        public int ConnectPort { get; set; }
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