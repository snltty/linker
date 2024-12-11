using linker.config;

namespace linker.plugins.relay.server
{
    public sealed class RelayServerConfigTransfer
    {
        public string SecretKey => config.Data.Server.Relay.SecretKey;
        public RelayNodeInfo Node=> config.Data.Server.Relay.Distributed.Node;
        public RelayMasterInfo Master => config.Data.Server.Relay.Distributed.Master;


        private readonly FileConfig config;
        public RelayServerConfigTransfer(FileConfig config)
        {
            this.config = config;
        }
    }
}
