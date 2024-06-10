using cmonitor.config;
using cmonitor.tunnel.connection;
using MemoryPack;
using System.Net;

namespace cmonitor.plugins.relay.transport
{
    public interface ITransport
    {
        public string Name { get; }
        public RelayCompactType Type { get; }
        public TunnelProtocolType ProtocolType { get; }

        public Task<ITunnelConnection> RelayAsync(RelayInfo relayInfo);
        public Task<ITunnelConnection> OnBeginAsync(RelayInfo relayInfo);
    }

    [MemoryPackable]
    public sealed partial class RelayInfo
    {
        public string RemoteMachineId { get; set; }
        public string RemoteMachineName { get; set; }
        public string TransactionId { get; set; }
        public string SecretKey { get; set; }
        public string TransportName { get; set; }

        public ulong FlowingId { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint Server { get; set; }

        public bool SSL { get; set; } = true;
    }
}
