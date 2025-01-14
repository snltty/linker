using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardSyncSecretKey : ISync
    {
        public string Name => "SForwardSecretKey";

        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        public SForwardSyncSecretKey(ISForwardClientStore sForwardClientStore, ISerializer serializer)
        {
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(sForwardClientStore.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            sForwardClientStore.SetSecretKey(serializer.Deserialize<string>(data.Span));
        }
    }
}
