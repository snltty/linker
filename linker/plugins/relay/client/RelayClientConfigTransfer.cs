using linker.client.config;
using linker.config;

namespace linker.plugins.relay.client
{
    public sealed class RelayClientConfigTransfer
    {
        public string DefaultNodeId => runningConfig.Data.Relay.DefaultNodeId;
        public RelayServerInfo Server => config.Data.Client.Relay.Servers[0];

        private readonly FileConfig config;
        private readonly RunningConfig  runningConfig;
        public RelayClientConfigTransfer(FileConfig config, RunningConfig runningConfig)
        {
            this.config = config;
            this.runningConfig = runningConfig;
        }

        public void SetDefaultNodeId(string defaultNodeId)
        {
            runningConfig.Data.Relay.DefaultNodeId = defaultNodeId;
            runningConfig.Data.Update();
        }

        public void SetServer(RelayServerInfo server)
        {
            config.Data.Client.Relay.Servers = [server];
            runningConfig.Data.Update();
        }
        public void SetServerSecretKey(string secretKey)
        {
            foreach (var item in config.Data.Client.Relay.Servers)
            {
                item.SecretKey = secretKey;
            }
            runningConfig.Data.Update();
        }
    }
}
