using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.transport
{
    public interface ITransport
    {
        public string Name { get; }
        public ProtocolType TypeFlag { get; }

        /// <summary>
        /// 发送连接信息
        /// </summary>
        public Func<TunnelTransportInfo, Task<TunnelTransportInfo>> OnSendConnectBegin { get; set; }
        /// <summary>
        /// 收到连接信息
        /// </summary>
        public Action<TunnelTransportInfo> OnConnectBegin { get; set; }
        /// <summary>
        /// 开始连接，获得对方信息
        /// </summary>
        public Action<TunnelTransportInfo> OnConnecting { get; set; }
        /// <summary>
        /// 收到连接
        /// </summary>
        public Action<TransportState> OnConnected { get; set; }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="fromMachineName">你的名字</param>
        /// <param name="toMachineName">对方的名字</param>
        /// <param name="flagName">唯一值</param>
        /// <returns></returns>
        public Task<Socket> ConnectAsync(string fromMachineName, string toMachineName, string flagName);
        /// <summary>
        /// 收到开始连接
        /// </summary>
        /// <param name="tunnelTransportNoticeInfo"></param>
        /// <returns></returns>
        public Task<TunnelTransportInfo> OnBegin(TunnelTransportInfo tunnelTransportNoticeInfo);
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint FromLocal { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint FromRemote { get; set; }

        public string FromMachineName { get; set; }
        public string ToMachineName { get; set; }
        public string FromFlagName { get; set; }

        public ProtocolType TypeFlag { get; set; }

        public int RouteLevel { get; set; }
    }

    public sealed class TransportState
    {
        public string FromMachineName { get; set; }
        public string FromFlagName { get; set; }
        public string FromTypeName { get; set; }
        public ProtocolType TypeFlag { get; set; }

        public object ConnectedObject { get; set; }
    }

}
