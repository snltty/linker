using linker.libs;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using Microsoft.Extensions.DependencyInjection;
namespace linker.plugins.relay.client
{
    public sealed partial class RelayClientTypesLoader
    {
        public RelayClientTypesLoader(RelayClientTransfer relayTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var transports = types.Select(c => (IRelayClientTransport)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            relayTransfer.LoadTransports(transports);

            LoggerHelper.Instance.Info($"load relay transport:{string.Join(",", transports.Select(c => c.GetType().Name))}");
        }
    }
}
