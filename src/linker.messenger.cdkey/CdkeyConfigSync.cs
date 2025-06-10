using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.cdkey
{
    public sealed class CdkeyConfigSyncSecretKey : ISync
    {
        public string Name => "CdkeySecretKey";

        private readonly ISerializer serializer;
        private readonly ICdkeyClientStore cdkeyClientStore;
        public CdkeyConfigSyncSecretKey( ISerializer serializer, ICdkeyClientStore cdkeyClientStore)
        {
            this.serializer = serializer;
            this.cdkeyClientStore = cdkeyClientStore;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(cdkeyClientStore.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            cdkeyClientStore.SetSecretKey(serializer.Deserialize<string>(data.Span));
        }
    }
}
