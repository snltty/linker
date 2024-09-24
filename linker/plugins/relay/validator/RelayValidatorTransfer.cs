using linker.libs;
using linker.plugins.relay.transport;
using linker.plugins.signin.messenger;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.relay.validator
{
    public sealed class RelayValidatorTransfer
    {
        private List<IRelayValidator> startups;

        public RelayValidatorTransfer(ServiceProvider serviceProvider)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(typeof(IRelayValidator));
            startups = types.Select(c => serviceProvider.GetService(c) as IRelayValidator).Where(c => c != null).ToList();
        }

        public async Task<string> Validate(RelayInfo relayInfo, SignCacheInfo cache, SignCacheInfo cache1)
        {
            foreach (var item in startups)
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
