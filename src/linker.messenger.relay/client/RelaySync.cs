using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.relay.client
{
    public sealed class RelaySyncSecretKey : ISync
    {
        public string Name => "RelaySecretKey";

        private readonly IRelayClientStore relayClientStore;
        private readonly ISerializer serializer;
        public RelaySyncSecretKey(IRelayClientStore relayClientStore, ISerializer serializer)
        {
            this.relayClientStore = relayClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(relayClientStore.Server.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            string value = serializer.Deserialize<string>(data.Span);
            relayClientStore.SetServerSecretKey(value);
        }
    }
}
