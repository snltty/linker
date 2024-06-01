using cmonitor.client.tunnel;
using MemoryPack;
using System.Net;

namespace cmonitor.plugins.tunnel.transport
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
        /// 收到开始连接
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo);
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportExternalIPRequestInfo
    {
        public string RemoteMachineName { get; set; }
        public TunnelProtocolType TransportType { get; set; }
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportExternalIPInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint Local { get; set; }

        [MemoryPackAllowSerialize]
        public IPAddress[] LocalIps { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint Remote { get; set; }

        public int RouteLevel { get; set; }

        public string MachineName { get; set; }
    }


    [MemoryPackable]
    public sealed partial class TunnelTransportRouteLevelInfo
    {
        public string MachineName { get; set; }
        public int RouteLevel { get; set; } = 0;
        public int RouteLevelPlus { get; set; } = 0;
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportItemInfo
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string ProtocolType { get; set; }

        public bool Disabled { get; set; }
        public bool Reverse { get; set; }
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



    [MemoryPackable]
    public sealed partial class TunnelTransportInfo
    {
        public TunnelTransportExternalIPInfo Local { get; set; }
        public TunnelTransportExternalIPInfo Remote { get; set; }

        public string TransactionId { get; set; }

        public TunnelProtocolType TransportType { get; set; }
        public string TransportName { get; set; }

        public TunnelDirection Direction { get; set; }
    }


}
