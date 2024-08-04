using System.Net;

namespace linker.tun
{
    /// <summary>
    /// 设备接口
    /// </summary>
    public interface ILinkerTunDevice
    {
        /// <summary>
        /// 设备名
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool Running { get; }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="address"></param>
        /// <param name="gateway"></param>
        /// <param name="prefixLength"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool SetUp(IPAddress address, IPAddress gateway, byte prefixLength, out string error);
        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown();

        /// <summary>
        /// 设置MTU
        /// </summary>
        /// <param name="value"></param>
        public void SetMtu(int value);
        /// <summary>
        /// 设置NAT转发
        /// </summary>
        public void SetNat();
        /// <summary>
        /// 移除NAT转发
        /// </summary>
        public void RemoveNat();

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        /// <param name="ip"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip);
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ip"></param>
        public void DelRoute(LinkerTunDeviceRouteItem[] ip);

        /// <summary>
        /// 读取数据包
        /// </summary>
        /// <returns></returns>
        public ReadOnlyMemory<byte> Read();
        /// <summary>
        /// 写入数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool Write(ReadOnlyMemory<byte> buffer);
    }

    /// <summary>
    /// 网卡读取数据回调
    /// </summary>
    public interface ILinkerTunDeviceCallback
    {
        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public Task Callback(LinkerTunDevicPacket packet);
    }

    /// <summary>
    /// 数据包
    /// </summary>
    public struct LinkerTunDevicPacket
    {
        /// <summary>
        /// 协议版本，4或者6
        /// </summary>
        public byte Version;
        /// <summary>
        /// 源IP
        /// </summary>
        public ReadOnlyMemory<byte> SourceIPAddress;
        /// <summary>
        /// 目标IP
        /// </summary>
        public ReadOnlyMemory<byte> DistIPAddress;
        /// <summary>
        /// 带4字节头的包
        /// </summary>
        public ReadOnlyMemory<byte> Packet;
        /// <summary>
        /// 原始IP包
        /// </summary>
        public ReadOnlyMemory<byte> IPPacket;
    }

    /// <summary>
    /// 添加路由项
    /// </summary>
    public sealed class LinkerTunDeviceRouteItem
    {
        /// <summary>
        /// IP
        /// </summary>
        public IPAddress Address { get; set; }
        /// <summary>
        /// 掩码
        /// </summary>
        public byte PrefixLength { get; set; }
    }

    /// <summary>
    /// 设备状态
    /// </summary>
    public enum LinkerTunDeviceStatus
    {
        /// <summary>
        /// 无
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 正在操作
        /// </summary>
        Operating = 1,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 2
    }
}
