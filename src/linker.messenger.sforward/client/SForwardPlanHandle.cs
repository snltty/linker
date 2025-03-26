using linker.messenger.plan;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardPlanHandle : IPlanHandle
    {
        public string CategoryName => "sforward";


        public SForwardPlanHandle(PlanTransfer planTransfer)
        {
            //每分钟的30s执行一次start
            planTransfer.Add(new PlanStoreInfo { Category = CategoryName, Key = "1", Value = "1", Handle = "start", Method = PlanMethod.Cron, Rule = "30 * * * * *" });
            //每次start后10s执行一次stop
            planTransfer.Add(new PlanStoreInfo { Category = CategoryName, Key = "1", Value = "1", Handle = "stop", TriggerHandle = "start", Method = PlanMethod.Trigger, Rule = "?-?-? ?:?:10" });
        }
        public async Task HandleAsync(string handle, string key, string value)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] exec plan {CategoryName} {handle} {key}->{value}");
            await Task.CompletedTask;
        }
    }
}
