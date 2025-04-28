using linker.messenger.relay.server;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerStore : IRelayServerStore
    {
        private readonly FileConfig config;
        public RelayServerStore(FileConfig config)
        {
            this.config = config;
        }

        public bool ValidateSecretKey(string secretKey)
        {
            return string.IsNullOrWhiteSpace(config.Data.Server.Relay.SecretKey) || config.Data.Server.Relay.SecretKey == secretKey;
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
