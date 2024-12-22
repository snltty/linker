using linker.libs;
using linker.messenger.relay.client;
using Microsoft.Extensions.DependencyInjection;
namespace linker.plugins.relay.client
{
    public sealed partial class RelayClientTypesLoader
    {
        public RelayClientTypesLoader(RelayClientTransfer relayTransfer, ServiceProvider serviceProvider)
        {
            LoggerHelper.Instance.Info($"load relay transport:{string.Join(",", relayTransfer.Transports.Select(c => c.GetType().Name))}");
        }
    }
}
