using linker.libs;
using Microsoft.Extensions.DependencyInjection;
namespace linker.plugins.flow
{
    public sealed partial class FlowTypesLoader
    {
        public FlowTypesLoader(FlowTransfer flowTransfer, ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            var flows = types.Select(c => (IFlow)serviceProvider.GetService(c)).Where(c => c != null).ToList();
            flowTransfer.LoadFlows(flows);

            LoggerHelper.Instance.Info($"load flows :{string.Join(",", flows.Select(c => c.GetType().Name))}");
        }
    }
}
