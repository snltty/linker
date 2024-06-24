using linker.tunnel.connection;
using System.Net;

namespace linker.tunnel.transport
{
    public interface ITunnelTransport
    {
        public string Name { get; }
        public string Label { get; }
        public TunnelProtocolType ProtocolType { get; }

        /// <summary>
        /// 发送连接开始信息
        /// </summary>
        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; }
        /// <summary>
        /// 发送连接失败消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; }
        /// <summary>
        /// 发送连接成功消息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; }
        /// <summary>
        /// 收到连接
        /// </summary>
        public Action<ITunnelConnection> OnConnected { get; set; }

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
    }

    public sealed partial class TunnelTransportItemInfo
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string ProtocolType { get; set; }

        public bool Disabled { get; set; } = false;
        public bool Reverse { get; set; } = true;
        public bool SSL { get; set; } = true;
    }
    public sealed class TunnelTransportItemInfoEqualityComparer : IEqualityComparer<TunnelTransportItemInfo>
    {
        public bool Equals(TunnelTransportItemInfo x, TunnelTransportItemInfo y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(TunnelTransportItemInfo obj)
        {
            return obj.Name.GetHashCode();
        }
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

        public List<IPEndPoint> RemoteEndPoints { get; set; }
    }


}
