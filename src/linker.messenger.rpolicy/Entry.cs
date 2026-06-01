using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.rpolicy
{
    public static class Entry
    {
        public static ServiceCollection AddRouteExclusionPolicy(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<RouteExclusionPolicyTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseRouteExclusionPolicy(this ServiceProvider serviceProvider)
        {
            RouteExclusionPolicyTransfer routeExclusionPolicyTransfer = serviceProvider.GetService<RouteExclusionPolicyTransfer>();
            return serviceProvider;
        }
    }
}
