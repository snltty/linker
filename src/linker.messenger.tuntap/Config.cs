using System.Collections.Concurrent;
using System.Net;

namespace linker.messenger.tuntap
{
    public sealed partial class TuntapConfigInfo
    {
        public TuntapConfigInfo() { }
        /// <summary>
        /// 网卡IP
        /// </summary>
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀长度
        /// </summary>
        public byte PrefixLength { get; set; } = 24;

        /// <summary>
        /// 局域网配置列表
        /// </summary>
        public List<TuntapLanInfo> Lans { get; set; } = new List<TuntapLanInfo>();

        public string Name { get; set; } = string.Empty;

        public Guid Guid { get; set; } = Guid.Parse("771EF382-8718-5BC5-EBF0-A28B86142278");

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        /// 开关，多个bool集合
        /// </summary>
        public TuntapSwitch Switch { get; set; }

        /// <summary>
        /// 端口转发列表
        /// </summary>
        public List<TuntapForwardInfo> Forwards { get; set; } = new List<TuntapForwardInfo>();

        public Dictionary<string, TuntapGroup2IPInfo> Group2IP { get; set; } = new Dictionary<string, TuntapGroup2IPInfo>();

        public bool DisableNat => (Switch & TuntapSwitch.DisableNat) == TuntapSwitch.DisableNat;
        public bool TcpMerge => (Switch & TuntapSwitch.TcpMerge) == TuntapSwitch.TcpMerge;
        public bool InterfaceOrder => (Switch & TuntapSwitch.InterfaceOrder) == TuntapSwitch.InterfaceOrder;
        public bool Multicast => (Switch & TuntapSwitch.Multicast) == TuntapSwitch.Multicast;
        public bool FakeAck => (Switch & TuntapSwitch.FakeAck) == TuntapSwitch.FakeAck;
        public bool SrcProxy => (Switch & TuntapSwitch.SrcProxy) == TuntapSwitch.SrcProxy;
    }

    public sealed class TuntapGroup2IPInfo
    {
        public IPAddress IP { get; set; } = IPAddress.Any;
        public byte PrefixLength { get; set; } = 24;
    }

    public sealed partial class TuntapVeaLanIPAddress
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
    public sealed partial class TuntapVeaLanIPAddressList
    {
        public string MachineId { get; set; }
        public List<TuntapVeaLanIPAddress> IPS { get; set; }

    }


    public partial class TuntapInfo
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
        public IPAddress IP { get; set; } = IPAddress.Any;
        /// <summary>
        /// 前缀长度
        /// </summary>
        public byte PrefixLength { get; set; } = 24;
        /// <summary>
        /// 网卡名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 网卡安装错误
        /// </summary>
        public string SetupError { get; set; }
        /// <summary>
        /// NAT设置错误
        /// </summary>
        public string NatError { get; set; }
        /// <summary>
        /// 系统信息
        /// </summary>
        public string SystemInfo { get; set; }

        /// <summary>
        /// 局域网IP列表
        /// </summary>
        public List<TuntapLanInfo> Lans { get; set; } = new List<TuntapLanInfo>();
        public IPAddress Wan { get; set; } = IPAddress.Any;
        /// <summary>
        /// 端口转发列表
        /// </summary>
        
        public List<TuntapForwardInfo> Forwards { get; set; } = new List<TuntapForwardInfo>();
        /// <summary>
        /// 开关，多个bool集合
        /// </summary>
        public TuntapSwitch Switch { get; set; }
        /// <summary>
        /// 延迟ms
        /// </summary>
        public int Delay { get; set; } = -1;
        /// <summary>
        /// 有效的
        /// </summary>
        public bool Available { get; set; } = true;
        /// <summary>
        /// 冲突
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// 是否网关
        /// </summary>
        public bool Gateway
        {
            get
            {
                return (Switch & TuntapSwitch.Gateway) == TuntapSwitch.Gateway;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.Gateway;
                }
                else
                {
                    Switch &= ~TuntapSwitch.Gateway;
                }
            }
        }
        /// <summary>
        /// 显示延迟
        /// </summary>
        public bool ShowDelay
        {
            get
            {
                return (Switch & TuntapSwitch.ShowDelay) == TuntapSwitch.ShowDelay;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.ShowDelay;
                }
                else
                {
                    Switch &= ~TuntapSwitch.ShowDelay;
                }
            }
        }
        /// <summary>
        /// 自动连接
        /// </summary>
        public bool AutoConnect
        {
            get
            {
                return (Switch & TuntapSwitch.AutoConnect) == TuntapSwitch.AutoConnect;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.AutoConnect;
                }
                else
                {
                    Switch &= ~TuntapSwitch.AutoConnect;
                }
            }
        }
        /// <summary>
        /// 使用高级功能
        /// </summary>
        public bool Upgrade
        {
            get
            {
                return (Switch & TuntapSwitch.Upgrade) == TuntapSwitch.Upgrade;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.Upgrade;
                }
                else
                {
                    Switch &= ~TuntapSwitch.Upgrade;
                }
            }
        }

        /// <summary>
        /// 使用广播
        /// </summary>
        public bool Multicast
        {
            get
            {
                return (Switch & TuntapSwitch.Multicast) == TuntapSwitch.Multicast;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.Multicast;
                }
                else
                {
                    Switch &= ~TuntapSwitch.Multicast;
                }
            }
        }
        /// <summary>
        /// 禁用Nat
        /// </summary>
        public bool DisableNat
        {
            get
            {
                return (Switch & TuntapSwitch.DisableNat) == TuntapSwitch.DisableNat;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.DisableNat;
                }
                else
                {
                    Switch &= ~TuntapSwitch.DisableNat;
                }
            }
        }
        /// <summary>
        /// tcp包合并
        /// </summary>
        public bool TcpMerge
        {
            get
            {
                return (Switch & TuntapSwitch.TcpMerge) == TuntapSwitch.TcpMerge;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.TcpMerge;
                }
                else
                {
                    Switch &= ~TuntapSwitch.TcpMerge;
                }
            }
        }
        /// <summary>
        /// 调整网卡顺序
        /// </summary>
        public bool InterfaceOrder
        {
            get
            {
                return (Switch & TuntapSwitch.InterfaceOrder) == TuntapSwitch.InterfaceOrder;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.InterfaceOrder;
                }
                else
                {
                    Switch &= ~TuntapSwitch.InterfaceOrder;
                }
            }
        }
        /// <summary>
        /// 是否开启了应用层NAT
        /// </summary>
        public bool AppNat => (Switch & TuntapSwitch.AppNat) == TuntapSwitch.AppNat;

        /// <summary>
        /// 使用伪ACK，测试用
        /// </summary>
        public bool FakeAck
        {
            get
            {
                return (Switch & TuntapSwitch.FakeAck) == TuntapSwitch.FakeAck;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.FakeAck;
                }
                else
                {
                    Switch &= ~TuntapSwitch.FakeAck;
                }
            }
        }

        /// <summary>
        /// 源代理
        /// </summary>
        public bool SrcProxy
        {
            get
            {
                return (Switch & TuntapSwitch.SrcProxy) == TuntapSwitch.SrcProxy;
            }
            set
            {
                if (value)
                {
                    Switch |= TuntapSwitch.SrcProxy;
                }
                else
                {
                    Switch &= ~TuntapSwitch.SrcProxy;
                }
            }
        }
    }


    public sealed partial class TuntapForwardInfo
    {
        public IPAddress ListenAddr { get; set; } = IPAddress.Any;
        public int ListenPort { get; set; }

        public IPAddress ConnectAddr { get; set; } = IPAddress.Any;
        public int ConnectPort { get; set; }

        public string Remark { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;
    }

    public sealed partial class TuntapForwardTestWrapInfo
    {
        public string MachineId { get; set; }
        public List<TuntapForwardTestInfo> List { get; set; }
    }
    public sealed partial class TuntapForwardTestInfo
    {
        public int ListenPort { get; set; }
        public IPAddress ConnectAddr { get; set; } = IPAddress.Any;
        public int ConnectPort { get; set; }
        public string Error { get; set; } = string.Empty;
    }


    public sealed partial class TuntapLanInfo
    {
        public TuntapLanInfo() { }
        public IPAddress IP { get; set; } = IPAddress.Any;
        public byte PrefixLength { get; set; } = 24;
        public bool Disabled { get; set; }
        public bool Exists { get; set; }
        public string Error { get; set; } = string.Empty;

        public IPAddress MapIP { get; set; } = IPAddress.Any;
        public byte MapPrefixLength { get; set; } = 24;

        public string Remark { get; set; } = string.Empty;
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
    [Flags]
    public enum TuntapSwitch
    {
        Gateway = 1,
        /// <summary>
        /// 启用显示延迟
        /// </summary>
        ShowDelay = 2,
        /// <summary>
        /// 启用高级功能
        /// </summary>
        Upgrade = 4,
        /// <summary>
        /// 启用自动连接
        /// </summary>
        AutoConnect = 8,
        /// <summary>
        /// 禁用广播
        /// </summary>
        Multicast = 16,
        /// <summary>
        /// 禁用Nat
        /// </summary>
        DisableNat = 32,
        /// <summary>
        /// 启用小包合并
        /// </summary>
        TcpMerge = 64,
        /// <summary>
        /// 调整网卡顺序
        /// </summary>
        InterfaceOrder = 128,

        /// <summary>
        /// 是否开启了应用层NAT
        /// </summary>
        AppNat = 256,

        /// <summary>
        /// 伪造ack
        /// </summary>
        FakeAck = 512,
        /// <summary>
        /// 源代理
        /// </summary>
        SrcProxy = 1024,
    }


    public sealed class TuntapConfigServerInfo
    {
        public TuntapLeaseConfigServerInfo Lease { get; set; } = new TuntapLeaseConfigServerInfo();
    }
    public sealed class TuntapLeaseConfigServerInfo
    {
        public int IPDays { get; set; } = 7;
        public int NetworkDays { get; set; } = 30;
    }
}

