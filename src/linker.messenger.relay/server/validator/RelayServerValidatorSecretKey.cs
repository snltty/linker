﻿using linker.messenger.signin;

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
            if (relayInfo.SecretKey != relayServerStore.SecretKey)
            {
                return $"SecretKey validate fail";
            }

            await Task.CompletedTask;
            return string.Empty;
        }
    }
}