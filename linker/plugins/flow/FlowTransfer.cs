using Microsoft.Extensions.DependencyInjection;
namespace linker.plugins.flow
{
    public sealed partial class FlowTransfer
    {
        private List<IFlow> flows = new List<IFlow>();

        public FlowTransfer(ServiceProvider serviceProvider)
        {
            var types = GetSourceGeneratorTypes();
            flows = types.Select(c => (IFlow)serviceProvider.GetService(c)).Where(c => c != null).ToList();
        }

        public Dictionary<string, FlowItemInfo> GetFlows()
        {
            return flows.Select(c => new FlowItemInfo { ReceiveBytes = c.ReceiveBytes, SendtBytes = c.SendtBytes, FlowName = c.FlowName }).ToDictionary(c => c.FlowName);
        }
    }

}
