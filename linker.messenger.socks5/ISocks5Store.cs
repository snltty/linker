using linker.libs;
using System.Net;

namespace linker.messenger.socks5
{
    public sealed partial class Socks5LanIPAddress
    {
        /// <summary>
        /// ip，存小端
        /// </summary>
        public uint IPAddress { get; set; }
        public byte PrefixLength { get; set; }
        public uint MaskValue { get; set; }
        public uint NetWork { get; set; }
        public uint Broadcast { get; set; }

        public string MachineId { get; set; }
        public IPAddress OriginIPAddress { get; set; }


    }

    public sealed partial class Socks5LanIPAddressList
    {
        public string MachineId { get; set; }
        public List<Socks5LanIPAddress> IPS { get; set; }

    }

    public sealed partial class Socks5Info
    {
        /// <summary>
        /// 设备id
        /// </summary>
        public string MachineId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public Socks5Status Status { get; set; }
        /// <summary>
        /// 前缀长度
        /// </summary>
        public int Port { get; set; } = 1804;

        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public List<Socks5LanInfo> Lans { get; set; } = new List<Socks5LanInfo>();

        /// <summary>
        /// 安装错误
        /// </summary>
        public string SetupError { get; set; }

        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();

    }

    public sealed partial class Socks5LanInfo
    {
        public IPAddress IP { get; set; }
        public byte PrefixLength { get; set; } = 24;
        public bool Disabled { get; set; }
        public bool Exists { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public enum Socks5Status : byte
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
    public interface ISocks5Store
    {
        public List<Socks5LanInfo> Lans { get; }
        public int Port { get; }
        public bool Running { get;}

        public void SetLans(List<Socks5LanInfo> lans);
        public void SetPort(int port);
        public void SetRunning(bool running);
    }
}
