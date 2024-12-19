using linker.config;
using linker.messenger.relay.server;

namespace linker.plugins.relay.server
{
    public sealed class RelayServerConfigTransfer
    {
        public string SecretKey => config.Data.Server.Relay.SecretKey;
        public RelayServerNodeInfo Node=> config.Data.Server.Relay.Distributed.Node;
        public RelayServerMasterInfo Master => config.Data.Server.Relay.Distributed.Master;


        private readonly FileConfig config;
        public RelayServerConfigTransfer(FileConfig config)
        {
            this.config = config;
        }

        public void SetMaxGbTotalMonth(int month)
        {
            Node.MaxGbTotalMonth = month;
        }
        public void SetMaxGbTotalLastBytes(ulong value)
        {
            Node.MaxGbTotalLastBytes = value;
        }
        public void Update()
        {
            config.Data.Update();
        }
    }
}
