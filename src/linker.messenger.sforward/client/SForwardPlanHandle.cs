using linker.messenger.plan;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardPlanHandle : IPlanHandle
    {
        public string CategoryName => "sforward";


        private readonly SForwardClientTransfer sForwardClientTransfer;
        public SForwardPlanHandle(SForwardClientTransfer sForwardClientTransfer)
        {
            this.sForwardClientTransfer = sForwardClientTransfer;
        }
        public async Task HandleAsync(string handle, string key, string value)
        {
            if (int.TryParse(key, out int id) == false) return;

            switch (handle)
            {
                case "start":
                    sForwardClientTransfer.Start(id);
                    break;
                case "stop":
                    sForwardClientTransfer.Stop(id);
                    break;
                default:
                    break;
            }

            await Task.CompletedTask;
        }
    }
}
