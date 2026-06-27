using linker.messenger.plan;

namespace linker.messenger.reverse.client
{
    public sealed class ReversePlanHandle : IPlanHandle
    {
        public string CategoryName => "Reverse";

        public string flag = "plan";

        private readonly ReverseClientTransfer ReverseClientTransfer;
        private readonly PlanTransfer planTransfer;
        public ReversePlanHandle(ReverseClientTransfer ReverseClientTransfer, PlanTransfer planTransfer)
        {
            this.ReverseClientTransfer = ReverseClientTransfer;
            this.planTransfer = planTransfer;

            ReverseClientTransfer.OnOpen += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "start"); };
            ReverseClientTransfer.OnClose += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "stop"); };
        }

        public async Task HandleAsync(string handle, string key, string value)
        {
            if (int.TryParse(key, out int id) == false) await Task.CompletedTask.ConfigureAwait(false);

            switch (handle)
            {
                case "start":
                    ReverseClientTransfer.Start(id, flag);
                    break;
                case "stop":
                    ReverseClientTransfer.Stop(id, flag);
                    break;
                default:
                    break;
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
