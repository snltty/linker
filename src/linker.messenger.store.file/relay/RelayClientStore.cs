using linker.messenger.relay.client;
using linker.tunnel.connection;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayClientStore : IRelayClientStore
    {
        public string DefaultNodeId => runningConfig.Data.Relay.DefaultNodeId;
        public TunnelProtocolType DefaultProtocol => runningConfig.Data.Relay.DefaultProtocol;

        
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;

        public RelayClientStore(FileConfig config, RunningConfig runningConfig)
        {
            this.config = config;
            this.runningConfig = runningConfig;
        }

        public void SetDefaultNodeId(string nodeId)
        {
            runningConfig.Data.Relay.DefaultNodeId = nodeId;
            runningConfig.Data.Update();
        }
        public void SetDefaultProtocol(TunnelProtocolType protocol)
        {
            runningConfig.Data.Relay.DefaultProtocol = protocol;
            runningConfig.Data.Update();
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }

     
    }
}
