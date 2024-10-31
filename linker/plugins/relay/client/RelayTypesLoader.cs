using linker.libs;
using linker.plugins.relay.client.transport;
using Microsoft.Extensions.DependencyInjection;
namespace linker.plugins.relay.client
{
    public sealed partial class RelayTypesLoader
    {
        public RelayTypesLoader(RelayTransfer relayTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var transports = types.Select(c => (ITransport)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            relayTransfer.LoadTransports(transports);

            LoggerHelper.Instance.Info($"load relay transport:{string.Join(",", transports.Select(c => c.GetType().Name))}");
        }
    }
}
