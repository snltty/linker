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
            return serializer.Serialize(new UpdaterSyncInfo
            {
                Sync2Server = updaterClientStore.Info.Sync2Server
            });
        }

        public void SetData(Memory<byte> data)
        {
            UpdaterSyncInfo info = serializer.Deserialize<UpdaterSyncInfo>(data.Span);
            updaterClientStore.SetSync2Server(info.Sync2Server);
        }
    }

    public sealed partial class UpdaterSyncInfo
    {
        /// <summary>
        /// 与服务器同步
        /// </summary>
        public bool Sync2Server { get; set; } = false;
    }
}
