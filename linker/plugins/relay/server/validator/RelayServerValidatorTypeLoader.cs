using linker.libs;
using linker.messenger.relay.server.validator;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.relay.server.validator
{
    public sealed partial class RelayServerValidatorTypeLoader
    {
        public RelayServerValidatorTypeLoader(RelayServerValidatorTransfer relayValidatorTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var validators = types.Select(c => (IRelayServerValidator)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            relayValidatorTransfer.LoadValidators(validators);

            LoggerHelper.Instance.Info($"load relay validators:{string.Join(",", validators.Select(c => c.GetType().Name))}");
        }
    }
}
