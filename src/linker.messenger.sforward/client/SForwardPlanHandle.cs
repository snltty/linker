using linker.messenger.plan;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardPlanHandle : IPlanHandle
    {
        public string CategoryName => "sforward";


        public SForwardPlanHandle()
        {
        }
        public async Task HandleAsync(string handle, string key, string value)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] exec plan {CategoryName} {handle} {key}->{value}");
            await Task.CompletedTask;
        }
    }
}
