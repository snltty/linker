using linker.libs;
using linker.libs.extends;
using linker.nat;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
namespace linker.tun.device
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
        /// <param name="info"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool Setup(LinkerTunDeviceSetupInfo info, out string error);
        /// <summary>
        /// 关闭
        /// </summary>
        public void Shutdown();

        /// <summary>
        /// 刷新网卡
        /// </summary>
        public void Refresh();

        /// <summary>
        /// 设置MTU
        /// </summary>
        /// <param name="value"></param>
        public void SetMtu(int value);
        /// <summary>
        /// 设置系统NAT转发
        /// </summary>
        public void SetNat(out string error);
        /// <summary>
        /// 移除NAT转发
        /// </summary>
        public void RemoveNat(out string error);


        /// <summary>
        /// 获取端口转发
        /// </summary>
        /// <returns></returns>
        public List<LinkerTunDeviceForwardItem> GetForward();
        /// <summary>
        /// 添加端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards);
        /// <summary>
        /// 删除端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards);

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips);
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips"></param>
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips);

        /// <summary>
        /// 读取数据包
        /// </summary>
        /// <returns></returns>
        public byte[] Read(out int length);
        /// <summary>
        /// 写入数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool Write(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// 检查网卡是否可用
        /// </summary>
        /// <returns></returns>
        public Task<bool> CheckAvailable(bool order = false);
    }

    public sealed class LinkerTunDeviceSetupInfo
    {
        /// <summary>
        /// 设备名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// IP地址
        /// </summary>
        public IPAddress Address { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀长度
        /// </summary>
        public byte PrefixLength { get; set; }
        /// <summary>
        /// GUID 仅windows
        /// </summary>
        public Guid Guid { get; set; } = Guid.Empty;

        /// <summary>
        /// MTU
        /// </summary>
        public int Mtu { get; set; } = 1420;
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
        public Task Callback(LinkerSrcProxyReadPacket proxy);
        public bool Callback(uint ip);
    }


    /// <summary>
    /// 网卡端口转发
    /// </summary>
    public sealed class LinkerTunDeviceForwardItem
    {
        public IPAddress ListenAddr { get; set; } = IPAddress.Any;
        public int ListenPort { get; set; }
        public IPAddress ConnectAddr { get; set; } = IPAddress.Any;
        public int ConnectPort { get; set; }

        [JsonIgnore]
        public bool Enable => ListenPort > 0 && ConnectAddr.Equals(IPAddress.Any) == false && ConnectPort > 0;

        public string Key => $"{ListenAddr}:{ListenPort}->{ConnectAddr}:{ConnectPort}";
    }
    public sealed class LinkerTunDeviceForwardItemComparer : IEqualityComparer<LinkerTunDeviceForwardItem>
    {
        public bool Equals(LinkerTunDeviceForwardItem x, LinkerTunDeviceForwardItem y)
        {
            return x.ListenPort == y.ListenPort && x.ConnectAddr.Equals(y.ConnectAddr) && x.ConnectPort == y.ConnectPort;
        }
        public int GetHashCode(LinkerTunDeviceForwardItem obj)
        {
            return obj.ListenPort.GetHashCode() ^ obj.ConnectAddr.GetHashCode() ^ obj.ConnectPort;
        }
    }

    /// <summary>
    /// 数据包
    /// </summary>
    public sealed class LinkerTunDevicPacket
    {
        public byte[] Buffer { get; private set; }
        public int Offset { get; private set; }
        public int Length { get; private set; }

        public Memory<byte> RawPacket => Buffer.AsMemory(Offset + 4, Length - 4);

        /// <summary>
        /// 协议版本，4或者6
        /// </summary>
        public byte Version { get; private set; }

        /// <summary>
        /// 协议
        /// </summary>
        public ProtocolType ProtocolType { get; private set; }

        /// <summary>
        /// 源IP
        /// </summary>
        public ReadOnlyMemory<byte> SrcIp { get; private set; }
        /// <summary>
        /// 源端口
        /// </summary>
        public ushort SrcPort { get; private set; }

        /// <summary>
        /// 目标IP
        /// </summary>
        public ReadOnlyMemory<byte> DstIp { get; private set; }

        /// <summary>
        /// 目标端口
        /// </summary>
        public ushort DstPort { get; private set; }

        public bool IPV4Broadcast => Version == 4 && DstIp.IsCast();
        public bool IPV6Multicast => Version == 6 && (DstIp.Span[0] & 0xFF) == 0xFF;

        public LinkerTunDevicPacket()
        {
        }
        public void Unpacket(byte[] buffer, int offset, int length, int pad = 4)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;

            ReadOnlyMemory<byte> ipPacket = Buffer.AsMemory(Offset + pad, Length - pad);
            Version = (byte)(ipPacket.Span[0] >> 4 & 0b1111);

            SrcIp = Helper.EmptyArray;
            DstIp = Helper.EmptyArray;

            if (Version == 4)
            {
                SrcIp = ipPacket.Slice(12, 4);
                DstIp = ipPacket.Slice(16, 4);

                ProtocolType = (ProtocolType)ipPacket.Span[9];

                if (ProtocolType == ProtocolType.Tcp || ProtocolType == ProtocolType.Udp)
                {
                    SrcPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(20, 2).ToUInt16());
                    DstPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(22, 2).ToUInt16());
                }
            }
            else if (Version == 6)
            {
                SrcIp = ipPacket.Slice(8, 16);
                DstIp = ipPacket.Slice(24, 16);

                ProtocolType = (ProtocolType)ipPacket.Span[6];

                if (ProtocolType == ProtocolType.Tcp || ProtocolType == ProtocolType.Udp)
                {
                    SrcPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(42, 2).ToUInt16());
                    DstPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(44, 2).ToUInt16());
                }
            }
        }

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

    public sealed class LinkerTunAppNatItemInfo
    {
        public IPAddress IP { get; set; }
        public byte PrefixLength { get; set; }
    }
}
