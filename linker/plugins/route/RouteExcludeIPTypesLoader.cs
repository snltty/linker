using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.route
{
    public sealed partial class RouteExcludeIPTypesLoader
    {
        public RouteExcludeIPTypesLoader(RouteExcludeIPTransfer tunnelExcludeIPTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var flows = types.Select(c => (IRouteExcludeIP)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            tunnelExcludeIPTransfer.LoadTunnelExcludeIPs(flows);

            LoggerHelper.Instance.Info($"load route excludeips :{string.Join(",", flows.Select(c => c.GetType().Name))}");
        }
    }
}
