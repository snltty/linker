using MemoryPack;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace cmonitor.plugins.tunnel.transport
{
    public interface ITransport
    {
        public string Name { get; }
        public ProtocolType Type { get; }

        /// <summary>
        /// 发送连接信息
        /// </summary>
        public Func<TunnelTransportInfo, Task> OnSendConnectBegin { get; set; }
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; }
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
        public Action<TunnelTransportState> OnConnected { get; set; }
        /// <summary>
        /// 断开连接
        /// </summary>
        public Action<TunnelTransportState> OnDisConnected { get; set; }

        public Action<string> OnConnectFail { get; set; }

        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo">你的名字</param>
        /// <returns></returns>
        public Task<TunnelTransportState> ConnectAsync(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 收到开始连接
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo);
        /// <summary>
        /// 对白
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo);
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportExternalIPRequestInfo
    {
        public string RemoteMachineName { get; set; }
        public ProtocolType TransportType { get; set; }
    }

    [MemoryPackable]
    public sealed partial class TunnelTransportExternalIPInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint Local { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint Remote { get; set; }

        public int RouteLevel { get; set; }

        public string MachineName { get; set; }
    }


    [MemoryPackable]
    public sealed partial class TunnelTransportInfo
    {
        public TunnelTransportExternalIPInfo Local { get; set; }
        public TunnelTransportExternalIPInfo Remote { get; set; }

        public string TransactionId { get; set; }

        public ProtocolType TransportType { get; set; }
        public string TransportName { get; set; }

        public TunnelTransportDirection Direction { get; set; }
    }




    public enum TunnelTransportDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public sealed class TunnelTransportState
    {
        public string RemoteMachineName { get; set; }
        public string TransactionId { get; set; }
        public string TransportName { get; set; }
        public ProtocolType TransportType { get; set; }

        [JsonIgnore]
        public object ConnectedObject { get; set; }
    }

}
