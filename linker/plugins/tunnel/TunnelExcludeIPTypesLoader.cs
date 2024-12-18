using linker.libs;
using linker.messenger.tunnel;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.tunnel
{
    public sealed partial class TunnelExcludeIPTypesLoader
    {
        public TunnelExcludeIPTypesLoader(TunnelExcludeIPTransfer tunnelExcludeIPTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var flows = types.Select(c => (ITunnelExcludeIP)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            tunnelExcludeIPTransfer.LoadTunnelExcludeIPs(flows);

            LoggerHelper.Instance.Info($"load tunnel excludeips :{string.Join(",", flows.Select(c => c.GetType().Name))}");
        }
    }
}
