using MemoryPack;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.tunnel.transport
{
    public interface ITransport
    {
        public string Name { get; }
        public ProtocolType TypeFlag { get; }
        public Func<TunnelTransportInfo, Task<TunnelTransportInfo>> SendBegin { get; set; }
        public Action<TransportState> OnConnected { get; set; }

        public Task<Socket> ConnectAsync(string fromMachineName, string toMachineName);
        public Task<TunnelTransportInfo> OnBegin(TunnelTransportInfo tunnelTransportNoticeInfo);
        public Task OnReverse(TunnelTransportInfo tunnelTransportNoticeInfo);
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

        public ProtocolType TypeFlag { get; set; }

        public int RouteLevel { get; set; }
    }

    public sealed class TransportState
    {
        public string FromMachineName { get; set; }
        public ProtocolType TypeFlag { get; set; }

        public object ConnectedObject { get; set; }
    }

}
