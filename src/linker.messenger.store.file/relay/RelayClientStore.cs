using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.signin;
using linker.tunnel.connection;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayClientStore : IRelayClientStore
    {
        public string DefaultNodeId => runningConfig.Data.Relay.DefaultNodeId;
        public TunnelProtocolType DefaultProtocol => runningConfig.Data.Relay.DefaultProtocol;
        public RelayServerInfo Server => config.Data.Client.Relay.Server;

        

        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;

        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;

        public RelayClientStore(SignInClientState signInClientState, ISignInClientStore signInClientStore, FileConfig config, RunningConfig runningConfig)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;

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

        public void SetServer(RelayServerInfo server)
        {
            config.Data.Client.Relay.Servers = [server];
            config.Data.Update();
        }
        public void SetServerSecretKey(string secretKey)
        {
            foreach (var item in config.Data.Client.Relay.Servers)
            {
                item.SecretKey = secretKey;
            }
            config.Data.Update();
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }

     
    }
}
