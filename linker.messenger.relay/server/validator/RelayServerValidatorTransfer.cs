using linker.messenger.signin;
using linker.messenger.relay.client.transport;

namespace linker.messenger.relay.server.validator
{
    public sealed partial class RelayServerValidatorTransfer
    {
        private List<IRelayServerValidator> validators;

        public RelayServerValidatorTransfer()
        {
        }

        public void LoadValidators(List<IRelayServerValidator> list)
        {
            validators = list;
        }

        public async Task<string> Validate(RelayInfo relayInfo, SignCacheInfo cache, SignCacheInfo cache1)
        {
            foreach (var item in validators)
            {
                string result = await item.Validate(relayInfo, cache, cache1);
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    return result;
                }
            }
            return string.Empty;
        }
    }
}
