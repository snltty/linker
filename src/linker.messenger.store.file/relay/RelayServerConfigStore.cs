using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerConfigStore : IRelayServerConfigStore
    {
        public int ServicePort => config.Data.Server.ServicePort;
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

        public void SetDataMonth(int month)
        {
            config.Data.Server.Relay.DataMonth = month;
        }

        public void SetShareKey(string shareKey)
        {
            config.Data.Server.Relay.ShareKey = shareKey;
        }
    }
}
