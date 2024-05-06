using cmonitor.plugins.relay.messenger;
using MemoryPack;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace cmonitor.plugins.relay.transport
{
    public interface ITransport
    {
        public string Name { get; }
        public Task<Socket> RelayAsync(RelayInfo relayInfo);
        public Task<Socket> OnBeginAsync(RelayInfo relayInfo);
    }

    [MemoryPackable]
    public sealed partial class RelayInfo
    {
        public string RemoteMachineName { get; set; }
        public string TransactionId { get; set; }
        public string SecretKey { get; set; }
        public string TransportName { get; set; }

        public ulong FlowingId { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint Server { get; set; }


    }

    public enum RelayTransportDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public sealed class RelayTransportState
    {
        public RelayInfo Info { get; set; }

        public RelayTransportDirection Direction { get; set; } = RelayTransportDirection.Reverse;

        public Socket Socket { get; set; }
    }
}
