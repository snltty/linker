using linker.config;
using linker.messenger.relay.server.validator;
using linker.messenger.signin;

namespace linker.plugins.relay.server.validator
{
    public sealed class RelayServerValidatorSecretKey : IRelayServerValidator
    {
        private readonly FileConfig fileConfig;
        private readonly RelayServerConfigTransfer relayServerConfigTransfer;
        public RelayServerValidatorSecretKey(FileConfig fileConfig, RelayServerConfigTransfer relayServerConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.relayServerConfigTransfer = relayServerConfigTransfer;
        }

        public async Task<string> Validate(linker.messenger.relay.client.transport.RelayInfo relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine)
        {
            if (relayInfo.SecretKey != relayServerConfigTransfer.SecretKey)
            {
                return $"SecretKey validate fail";
            }

            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
