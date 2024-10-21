using linker.libs;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.capi
{
    public sealed partial class ApiClientTypesLoader
    {
        public ApiClientTypesLoader(IApiClientServer apiClientServer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var flows = types.Select(c => serviceProvider.GetService(c)).Where(c => c != null).ToList();
            apiClientServer.LoadPlugins(flows);

            LoggerHelper.Instance.Info($"load client apis :{string.Join(",", flows.Select(c => c.GetType().Name))}");
        }
    }
}
