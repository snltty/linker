using linker.libs;

namespace linker.messenger.flow
{
    public sealed class FlowTransfer
    {
        private List<IFlow> flows = new List<IFlow>();
        public FlowTransfer()
        {
        }

        public void AddFlows(List<IFlow> list)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add flow {string.Join(",", list.Select(c => c.GetType().Name))}");
            this.flows = this.flows.Concat(list).Distinct().ToList();
        }

        public Dictionary<string, FlowItemInfo> GetFlows()
        {
            return flows.Select(c => new FlowItemInfo { ReceiveBytes = c.ReceiveBytes, SendtBytes = c.SendtBytes, FlowName = c.FlowName }).ToDictionary(c => c.FlowName);
        }
        public Dictionary<string, FlowItemInfo> GetDiffFlows(Dictionary<string, FlowItemInfo> oldDic)
        {
            return flows.Select(c =>
            {
                if (oldDic.TryGetValue(c.FlowName, out FlowItemInfo oldItem))
                {
                    var (recv, sendt) = c.GetDiffBytes(oldItem.ReceiveBytes, oldItem.SendtBytes);
                    return new FlowItemInfo { ReceiveBytes = recv, SendtBytes = sendt, FlowName = c.FlowName };
                }

                return new FlowItemInfo { ReceiveBytes = c.ReceiveBytes, SendtBytes = c.SendtBytes, FlowName = c.FlowName };
            }).ToDictionary(c => c.FlowName);
        }
    }

}
