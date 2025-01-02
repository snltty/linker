using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerMasterStore : IRelayServerMasterStore
    {
        public RelayServerMasterInfo Master => config.Data.Server.Relay.Distributed.Master;

        private readonly FileConfig config;
        public RelayServerMasterStore(FileConfig config)
        {
            this.config = config;
        }

        public void SetInfo(RelayServerMasterInfo info)
        {
            config.Data.Server.Relay.Distributed.Master = info;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }
}
