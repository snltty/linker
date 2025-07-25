using linker.libs;
using linker.libs.extends;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
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
    }

    /// <summary>
    /// 数据包钩子
    /// </summary>
    public interface ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level { get; }

        /// <summary>
        /// 从网卡读取到数据包后
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool ReadAfter(ReadOnlyMemory<byte> packet);
        /// <summary>
        /// 写入网卡前
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet);
    }
    /// <summary>
    /// 回调处理级别
    /// </summary>
    public enum LinkerTunPacketHookLevel
    {
        /// <summary>
        /// 最低的，也是最早执行的，不要用这个
        /// </summary>
        Lowest = int.MinValue,
        Low9 = -9,
        Low8 = -8,
        Low7 = -7,
        Low6 = -6,
        Low5 = -5,
        Low4 = -4,
        Low3 = -3,
        Low2 = -2,
        Low1 = -1,
        Normal = 0,
        High1 = 1,
        High2 = 2,
        High3 = 3,
        High4 = 4,
        High5 = 5,
        High6 = 6,
        High7 = 7,
        High8 = 8,
        High9 = 9,
        /// <summary>
        /// 最高的，也是最晚执行的，不要用这个
        /// </summary>
        Highest = int.MaxValue
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
        public ReadOnlyMemory<byte> SourceIPAddress { get; private set; }
        /// <summary>
        /// 源端口
        /// </summary>
        public ushort SourcePort { get; private set; }
        /// <summary>
        /// 源
        /// </summary>
        public IPEndPoint Source => new IPEndPoint(new IPAddress(SourceIPAddress.Span), SourcePort);

        /// <summary>
        /// 目标IP
        /// </summary>
        public ReadOnlyMemory<byte> DistIPAddress { get; private set; }
        /// <summary>
        /// 目标端口
        /// </summary>
        public ushort DistPort { get; private set; }
        /// <summary>
        /// 目标
        /// </summary>
        public IPEndPoint Dist => new IPEndPoint(new IPAddress(DistIPAddress.Span), DistPort);

        public bool IPV4Broadcast => Version == 4 && DistIPAddress.GetIsBroadcastAddress();
        public bool IPV6Multicast => Version == 6 && (DistIPAddress.Span[0] & 0xFF) == 0xFF;

        public LinkerTunDevicPacket()
        {
        }
        public LinkerTunDevicPacket(byte[] buffer, int offset, int length)
        {
            Unpacket(buffer, offset, length);
        }
        public void Unpacket(byte[] buffer, int offset, int length)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;

            ReadOnlyMemory<byte> packet = buffer;
            ReadOnlyMemory<byte> ipPacket = packet.Slice(4);
            Version = (byte)(ipPacket.Span[0] >> 4 & 0b1111);

            SourceIPAddress = Helper.EmptyArray;
            DistIPAddress = Helper.EmptyArray;

            if (Version == 4)
            {
                SourceIPAddress = ipPacket.Slice(12, 4);
                DistIPAddress = ipPacket.Slice(16, 4);

                ProtocolType = (ProtocolType)ipPacket.Span[9];
                if (ProtocolType == ProtocolType.Tcp || ProtocolType == ProtocolType.Udp)
                {
                    SourcePort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(20, 2).ToUInt16());
                    DistPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(22, 2).ToUInt16());
                }
            }
            else if (Version == 6)
            {
                SourceIPAddress = ipPacket.Slice(8, 16);
                DistIPAddress = ipPacket.Slice(24, 16);

                ProtocolType = (ProtocolType)ipPacket.Span[6];

                if (ProtocolType == ProtocolType.Tcp || ProtocolType == ProtocolType.Udp)
                {
                    SourcePort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(42, 2).ToUInt16());
                    DistPort = BinaryPrimitives.ReverseEndianness(ipPacket.Slice(44, 2).ToUInt16());
                }
            }
        }
    }
    public struct LinkerTunDevicValidatePacket
    {
        public bool IsValid { get; private set; }
        public LinkerTunDevicValidatePacket(ReadOnlyMemory<byte> packet)
        {
            Validate(packet);
        }
        private void Validate(ReadOnlyMemory<byte> packet)
        {
            if (packet.Length < 1)
            {
                return;
            }

            byte version = (byte)(packet.Span[0] >> 4 & 0b1111);
            int headLength = version == 4 ? (packet.Span[0] & 0b1111) * 4 : 40;
            if (packet.Length < headLength) return;

            ProtocolType protocolType = version switch
            {
                4 => (ProtocolType)packet.Span[9],
                6 => (ProtocolType)packet.Span[6],
                _ => ProtocolType.Unknown
            };
            IsValid = protocolType switch
            {
                ProtocolType.Tcp => packet.Length >= headLength + 20,
                ProtocolType.Udp => packet.Length >= headLength + 8,
                ProtocolType.Icmp => packet.Length >= headLength + 8,
                _ => false
            };
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
