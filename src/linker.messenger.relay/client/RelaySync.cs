using linker.libs;
using linker.messenger.sync;
using linker.tunnel.connection;

namespace linker.messenger.relay.client
{
    public sealed class RelaySyncDefault : ISync
    {
        public string Name => "RelayDefault";

        private readonly IRelayClientStore relayClientStore;
        private readonly ISerializer serializer;
        public RelaySyncDefault(IRelayClientStore relayClientStore, ISerializer serializer)
        {
            this.relayClientStore = relayClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(new KeyValuePair<string, TunnelProtocolType>(relayClientStore.DefaultNodeId, relayClientStore.DefaultProtocol));
        }

        public void SetData(Memory<byte> data)
        {
            KeyValuePair<string, TunnelProtocolType> value = serializer.Deserialize<KeyValuePair<string, TunnelProtocolType>>(data.Span);
            relayClientStore.SetDefaultNodeId(value.Key);
            relayClientStore.SetDefaultProtocol(value.Value);
        }
    }
}
