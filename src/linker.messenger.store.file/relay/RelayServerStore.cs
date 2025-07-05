using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerStore : IRelayServerStore
    {
        private readonly FileConfig config;
        public RelayServerStore(FileConfig config)
        {
            this.config = config;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }

     
    }
}
