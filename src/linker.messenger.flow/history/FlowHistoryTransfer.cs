using linker.libs.timer;
using linker.messenger.signin;

namespace linker.messenger.flow.history
{
    public sealed class FlowHistoryTransfer
    {
        private DateTime currentTime = default;

        private readonly FlowTransfer flowTransfer;
        private readonly SignInServerCaching signInServerCaching;
        private readonly IFlowHistoryStore flowHistoryStore;

        Dictionary<string, FlowItemInfo> dicOld = new Dictionary<string, FlowItemInfo>();

        public FlowHistoryTransfer(FlowTransfer flowTransfer, SignInServerCaching signInServerCaching, IFlowHistoryStore flowHistoryStore)
        {
            this.flowTransfer = flowTransfer;
            this.signInServerCaching = signInServerCaching;
            this.flowHistoryStore = flowHistoryStore;
        }

        private void FlowTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (currentTime == default || DateTime.Now.ToString("yyyy-MM-dd HH:mm") != currentTime.ToString("yyyy-MM-dd HH:mm"))
                {
                    currentTime = DateTime.Now;
                    Update();
                }
            }, 15000);
        }
        private void Update()
        {
            flowHistoryStore.Clear(30);

            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetDiffFlows(dicOld);
            foreach (var item in dic)
            {
                flowHistoryStore.Add(new FlowHistoryInfo
                {
                    Key = item.Key,
                    Time = currentTime,
                    Recv = item.Value.ReceiveBytes,
                    Sendt = item.Value.SendtBytes,
                });
            }

            signInServerCaching.GetOnline(out int all, out int online);
            flowHistoryStore.Add(new FlowHistoryInfo
            {
                Key = "Online",
                Time = currentTime,
                Recv = all,
                Sendt = online,
            });

            dicOld = dic;
        }

    }
}
