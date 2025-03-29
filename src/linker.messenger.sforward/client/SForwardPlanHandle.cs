using linker.messenger.plan;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardPlanHandle : IPlanHandle
    {
        public string CategoryName => "sforward";

        public string flag = "plan";

        private readonly SForwardClientTransfer sForwardClientTransfer;
        private readonly PlanTransfer planTransfer;
        public SForwardPlanHandle(SForwardClientTransfer sForwardClientTransfer, PlanTransfer planTransfer)
        {
            this.sForwardClientTransfer = sForwardClientTransfer;
            this.planTransfer = planTransfer;

            sForwardClientTransfer.OnOpen += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "start"); };
            sForwardClientTransfer.OnClose += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "stop"); };
        }

        public async Task HandleAsync(string handle, string key, string value)
        {
            if (int.TryParse(key, out int id) == false) return;

            switch (handle)
            {
                case "start":
                    sForwardClientTransfer.Start(id, flag);
                    break;
                case "stop":
                    sForwardClientTransfer.Stop(id, flag);
                    break;
                default:
                    break;
            }

            await Task.CompletedTask;
        }
    }
}
