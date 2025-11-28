using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerConfigStore : IRelayServerConfigStore
    {
        public RelayServerConfigInfo Config => config.Data.Server.Relay;

        private readonly FileConfig config;
        public RelayServerConfigStore(FileConfig config)
        {
            this.config = config;
        }

        public void Confirm()
        {
            config.Data.Update();
        }

        public void SetInfo(RelayServerConfigInfo node)
        {
            config.Data.Server.Relay = node;
        }
        public void SetDataRemain(long value)
        {
            config.Data.Server.Relay.DataRemain = value;
        }

        public void DataMonth(int month)
        {
            config.Data.Server.Relay.DataMonth = month;
        }
    }
}
