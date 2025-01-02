using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerNodeStore : IRelayServerNodeStore
    {
        public int ServicePort => config.Data.Server.ServicePort;
        public RelayServerNodeInfo Node => config.Data.Server.Relay.Distributed.Node;

        private readonly FileConfig config;
        public RelayServerNodeStore(FileConfig config)
        {
            this.config = config;
        }

        public void Confirm()
        {
            config.Data.Update();
        }

        public void SetInfo(RelayServerNodeInfo node)
        {
            config.Data.Server.Relay.Distributed.Node = node;
        }
        public void SetMaxGbTotalLastBytes(ulong value)
        {
            config.Data.Server.Relay.Distributed.Node.MaxGbTotalLastBytes=value;
        }

        public void SetMaxGbTotalMonth(int month)
        {
            config.Data.Server.Relay.Distributed.Node.MaxGbTotalMonth = month;
        }

      
    }
}
