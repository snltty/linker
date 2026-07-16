using linker.messenger.plan;

namespace linker.messenger.reverse.client
{
    public sealed class ReversePlanHandle : IPlanHandle
    {
        public string CategoryName => "reverse";

        public string flag = "plan";

        private readonly ReverseClientTransfer reverseClientTransfer;
        public ReversePlanHandle(ReverseClientTransfer reverseClientTransfer, PlanTransfer planTransfer)
        {
            this.reverseClientTransfer = reverseClientTransfer;

            reverseClientTransfer.OnOpen += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "start"); };
            reverseClientTransfer.OnClose += (id, _flag) => { if (_flag != flag) planTransfer.Trigger(CategoryName, id.ToString(), "stop"); };
        }

        public async Task HandleAsync(string handle, string key, string value)
        {
            if (int.TryParse(key, out int id) == false) await Task.CompletedTask.ConfigureAwait(false);

            switch (handle)
            {
                case "start":
                    reverseClientTransfer.Start(id, flag);
                    break;
                case "stop":
                    reverseClientTransfer.Stop(id, flag);
                    break;
                default:
                    break;
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
