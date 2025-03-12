
using Microsoft.Extensions.DependencyInjection;

namespace linker.messenger.plan
{
    public static class Entry
    {
        public static ServiceCollection AddPlan(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PlanTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UsePlan(this ServiceProvider serviceProvider)
        {
            PlanTransfer planTransfer = serviceProvider.GetService<PlanTransfer>();
            return serviceProvider;
        }

    }
}
