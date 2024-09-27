using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace linker.plugins.flow
{
    public sealed class FlowTransfer
    {
        private List<IFlow> flows = new List<IFlow>();


        private readonly ServiceProvider serviceProvider;
        public FlowTransfer(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public void LoadFlows(Assembly[] assemblys)
        {
            var types = ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFlow)).Distinct();
            flows = types.Select(c=> (IFlow)serviceProvider.GetService(c)).Where(c=>c != null).ToList();
        }

        public Dictionary<string, FlowItemInfo> GetFlows()
        {
            return flows.Select(c => new FlowItemInfo { ReceiveBytes = c.ReceiveBytes, SendtBytes = c.SendtBytes, FlowName = c.FlowName }).ToDictionary(c => c.FlowName);
        }
    }
}
