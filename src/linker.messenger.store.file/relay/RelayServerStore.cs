using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerStore : IRelayServerStore
    {
        public string SecretKey => config.Data.Server.Relay.SecretKey;
        private readonly FileConfig config;
        public RelayServerStore(FileConfig config)
        {
            this.config = config;
        }

        public void SetSecretKey(string secretKey)
        {
            config.Data.Server.Relay.SecretKey = secretKey;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }
}
