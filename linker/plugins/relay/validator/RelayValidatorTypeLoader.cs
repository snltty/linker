using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.relay.validator
{
    public sealed partial class RelayValidatorTypeLoader
    {
        public RelayValidatorTypeLoader(RelayValidatorTransfer relayValidatorTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var validators = types.Select(c => (IRelayValidator)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            relayValidatorTransfer.LoadValidators(validators);

            LoggerHelper.Instance.Info($"load relay validators:{string.Join(",", validators.Select(c => c.GetType().Name))}");
        }
    }
}
