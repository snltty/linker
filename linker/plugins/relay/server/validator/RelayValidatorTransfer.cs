using linker.plugins.relay.client.transport;
using linker.plugins.signin.messenger;

namespace linker.plugins.relay.server.validator
{
    public sealed partial class RelayValidatorTransfer
    {
        private List<IRelayValidator> validators;

        public RelayValidatorTransfer()
        {
        }

        public void LoadValidators(List<IRelayValidator> list)
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
