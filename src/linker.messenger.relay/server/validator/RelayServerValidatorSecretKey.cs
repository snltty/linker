using linker.messenger.signin;

namespace linker.messenger.relay.server.validator
{
    public sealed class RelayServerValidatorSecretKey : IRelayServerValidator
    {
        public string Name => "secretKey";

        private readonly IRelayServerStore relayServerStore;
        public RelayServerValidatorSecretKey(IRelayServerStore relayServerStore)
        {
            this.relayServerStore = relayServerStore;
        }

       
        public async Task<string> Validate(linker.messenger.relay.client.transport.RelayInfo relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine)
        {
            if (relayServerStore.ValidateSecretKey(relayInfo.SecretKey) == false)
            {
                return $"SecretKey validate fail";
            }

            await Task.CompletedTask.ConfigureAwait(false);
            return string.Empty;
        }
    }
}
