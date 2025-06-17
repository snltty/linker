using linker.libs;
using linker.messenger.sync;

namespace linker.messenger.updater
{
    public sealed class UpdaterConfigSyncSecretKey : ISync
    {
        public string Name => "UpdaterSecretKey";

        private readonly UpdaterClientTransfer updaterClientTransfer;
        private readonly ISerializer serializer;
        private readonly IUpdaterClientStore updaterClientStore;
        public UpdaterConfigSyncSecretKey(UpdaterClientTransfer updaterClientTransfer, ISerializer serializer, IUpdaterClientStore updaterClientStore)
        {
            this.updaterClientTransfer = updaterClientTransfer;
            this.serializer = serializer;
            this.updaterClientStore = updaterClientStore;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(updaterClientStore.Info);
        }

        public void SetData(Memory<byte> data)
        {
            UpdaterConfigClientInfo info = serializer.Deserialize<UpdaterConfigClientInfo>(data.Span);
            updaterClientStore.SetSecretKey(info.SecretKey);
            updaterClientStore.SetSync2Server(info.Sync2Server);
        }
    }
}
