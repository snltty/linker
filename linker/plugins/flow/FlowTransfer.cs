namespace linker.plugins.flow
{
    public sealed class FlowTransfer
    {
        private List<IFlow> flows = new List<IFlow>();
        public FlowTransfer()
        {
        }

        public void LoadFlows(List<IFlow> flows)
        {
            this.flows = flows;
        }

        public Dictionary<string, FlowItemInfo> GetFlows()
        {
            return flows.Select(c => new FlowItemInfo { ReceiveBytes = c.ReceiveBytes, SendtBytes = c.SendtBytes, FlowName = c.FlowName }).ToDictionary(c => c.FlowName);
        }
    }

}
