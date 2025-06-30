using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.wlist
{
    public sealed class WhitelistConfigSyncSecretKey : ISync
    {
        public string Name => "WhiteListSecretKey";

        private readonly ISerializer serializer;
        private readonly IWhiteListClientStore whiteListClientStore;
        public WhitelistConfigSyncSecretKey(ISerializer serializer, IWhiteListClientStore whiteListClientStore)
        {
            this.serializer = serializer;
            this.whiteListClientStore = whiteListClientStore;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(whiteListClientStore.SecretKey);
        }

        public void SetData(Memory<byte> data)
        {
            whiteListClientStore.SetSecretKey(serializer.Deserialize<string>(data.Span));
        }
    }
}
