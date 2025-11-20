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

        public IPAddress Wan { get; set; } = IPAddress.Any;

    }

    public sealed partial class Socks5LanInfo
    {
        public IPAddress IP { get; set; } = IPAddress.Any;
        public byte PrefixLength { get; set; } = 24;
        public IPAddress MapIP { get; set; } = IPAddress.Any;
        public byte MapPrefixLength { get; set; } = 24;
        public bool Disabled { get; set; }
        public bool Exists { get; set; }
        public string Error { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
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
        /// <summary>
        /// 局域网IP
        /// </summary>
        public List<Socks5LanInfo> Lans { get; }
        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get; }
        /// <summary>
        /// 是否启动
        /// </summary>
        public bool Running { get;}
        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// 设置局域网ip
        /// </summary>
        /// <param name="lans"></param>
        public void SetLans(List<Socks5LanInfo> lans);
        /// <summary>
        /// 设置监听端口
        /// </summary>
        /// <param name="port"></param>
        public void SetPort(int port);
        /// <summary>
        /// 设置启动状态
        /// </summary>
        /// <param name="running"></param>
        /// <param name="error"></param>
        public void SetRunning(bool running,string error);
        /// <summary>
        /// 提交保存
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
