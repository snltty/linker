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
        public void UpdateInfo(RelayServerNodeUpdateInfo188 update)
        {
            config.Data.Server.Relay.Distributed.Node.Name = update.Name;
            config.Data.Server.Relay.Distributed.Node.MaxConnection = update.MaxConnection;
            config.Data.Server.Relay.Distributed.Node.MaxBandwidth = update.MaxBandwidth;
            config.Data.Server.Relay.Distributed.Node.MaxBandwidthTotal = update.MaxBandwidthTotal;
            config.Data.Server.Relay.Distributed.Node.MaxGbTotal = update.MaxGbTotal;
            config.Data.Server.Relay.Distributed.Node.MaxGbTotalLastBytes = update.MaxGbTotalLastBytes;
            config.Data.Server.Relay.Distributed.Node.Public = update.Public;
            config.Data.Server.Relay.Distributed.Node.Url = update.Url;
            config.Data.Server.Relay.Distributed.Node.AllowTcp = update.AllowTcp;
            config.Data.Server.Relay.Distributed.Node.AllowUdp = update.AllowUdp;
            config.Data.Server.Relay.Distributed.Node.Sync2Server = update.Sync2Server;

        }
        public void SetMaxGbTotalLastBytes(long value)
        {
            config.Data.Server.Relay.Distributed.Node.MaxGbTotalLastBytes = value;
        }

        public void SetMaxGbTotalMonth(int month)
        {
            config.Data.Server.Relay.Distributed.Node.MaxGbTotalMonth = month;
        }


    }
}
