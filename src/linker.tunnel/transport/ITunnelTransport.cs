using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.transport
{
    public interface ITunnelTransport
    {
        /// <summary>
        /// 打洞协议名
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 打洞协议说明
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// 隧道协议
        /// </summary>
        public TunnelProtocolType ProtocolType { get; }
        /// <summary>
        /// 允许哪些端口协议
        /// </summary>
        public TunnelWanPortProtocolType AllowWanPortProtocolType { get; }
        /// <summary>
        /// 是否反向打洞
        /// </summary>
        public bool Reverse { get; }
        /// <summary>
        /// 是否允许修改反向打洞配置
        /// </summary>
        public bool DisableReverse { get; }
        /// <summary>
        /// 是否ssl
        /// </summary>
        public bool SSL { get; }
        /// <summary>
        /// 是否允许修改ssl配置
        /// </summary>
        public bool DisableSSL { get; }

        /// <summary>
        /// 默认排序
        /// </summary>
        public byte Order { get; }

        /// <summary>
        /// 收到连接
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; }

        public void SetSSL(X509Certificate certificate);

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo">你的名字</param>
        /// <returns></returns>
        public Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 收到开始打洞
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public Task OnBegin(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 收到打洞失败
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 收到打洞成功
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo);
    }

    /// <summary>
    /// 网络信息，包括局域网，外网
    /// </summary>
    public sealed partial class TunnelTransportWanPortInfo
    {
        /// <summary>
        /// 我的本地
        /// </summary>
        public IPEndPoint Local { get; set; }
        /// <summary>
        /// 我的外网
        /// </summary>
        public IPEndPoint Remote { get; set; }
        /// <summary>
        /// 我的局域网IP
        /// </summary>
        public IPAddress[] LocalIps { get; set; }

        /// <summary>
        /// 我的外网层级
        /// </summary>
        public int RouteLevel { get; set; }

        /// <summary>
        /// 我的id
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 我的名称
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// 固定端口，外网
        /// </summary>
        public int PortMapWan { get; set; }
        /// <summary>
        /// 固定端口，内网
        /// </summary>
        public int PortMapLan { get; set; }
    }

    public sealed partial class TunnelTransportItemInfo
    {
        public TunnelTransportItemInfo() { }
        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 协议描述
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 协议
        /// </summary>
        public string ProtocolType { get; set; }
        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// 是否反向打洞
        /// </summary>
        public bool Reverse { get; set; } = true;
        /// <summary>
        /// 禁止修改反向打洞配置
        /// </summary>
        public bool DisableReverse { get; set; }
        /// <summary>
        /// 是否开启ssl
        /// </summary>
        public bool SSL { get; set; } = true;
        /// <summary>
        /// 禁止修改ssl配置
        /// </summary>
        public bool DisableSSL { get; set; }
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        public byte BufferSize { get; set; } = 4;
        /// <summary>
        /// 排序
        /// </summary>
        public byte Order { get; set; }

        public Addrs Addr { get; set; } = Addrs.Ipv6 | Addrs.Ipv4 | Addrs.Lan;
    }

    public enum Addrs : byte
    {
        Ipv6 = 1,
        Ipv4 = 2,
        Lan = 4,
    }


    public sealed partial class TunnelTransportInfo
    {
        /// <summary>
        /// 我的
        /// </summary>
        public TunnelTransportWanPortInfo Local { get; set; }
        /// <summary>
        /// 对方的
        /// </summary>
        public TunnelTransportWanPortInfo Remote { get; set; }

        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 协议类型
        /// </summary>
        public TunnelProtocolType TransportType { get; set; }
        /// <summary>
        /// 协议名
        /// </summary>
        public string TransportName { get; set; }
        /// <summary>
        /// 方向
        /// </summary>
        public TunnelDirection Direction { get; set; }
        /// <summary>
        /// 需要加密
        /// </summary>
        public bool SSL { get; set; }
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;

        /// <summary>
        /// 流id
        /// </summary>
        public uint FlowId { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public string TransactionTag { get; set; }

        /// <summary>
        /// 目标ip列表
        /// </summary>
        public List<IPEndPoint> RemoteEndPoints { get; set; }
    }


    public sealed class TunnelExIPInfo
    {
        public IPAddress IP { get; set; }
        public byte PrefixLength { get; set; }
    }


    public sealed class NetworkInfo
    {
        /// <summary>
        /// 本机局域网IP列表，可以通过NetworkHelper.GetRouteLevel 获取
        /// </summary>
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 本机与外网的距离，通过多少网关，可以通过NetworkHelper.GetRouteLevel 获取
        /// </summary>
        public int RouteLevel { get; set; }
        /// <summary>
        /// 本机名
        /// </summary>
        public string MachineId { get; set; }
    }

    public sealed class TunnelWanPortProtocolInfo
    {
        /// <summary>
        /// 协议
        /// </summary>
        public TunnelWanPortProtocolType ProtocolType { get; set; } = TunnelWanPortProtocolType.Udp;
        /// <summary>
        /// 对方id
        /// </summary>
        public string MachineId { get; set; }
    }
}
