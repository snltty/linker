using cmonitor.tunnel.connection;
using System.Net;

namespace cmonitor.tunnel.transport
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
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo);
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

    public sealed partial class TunnelTransportExternalIPInfo
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }

        public IPAddress[] LocalIps { get; set; }

        public int RouteLevel { get; set; }

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
        public TunnelTransportExternalIPInfo Local { get; set; }
        public TunnelTransportExternalIPInfo Remote { get; set; }

        public string TransactionId { get; set; }

        public TunnelProtocolType TransportType { get; set; }
        public string TransportName { get; set; }

        public TunnelDirection Direction { get; set; }

        public bool SSL { get; set; }
    }


}
