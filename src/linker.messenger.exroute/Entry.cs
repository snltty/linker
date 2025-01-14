using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.exroute
{
    public static class Entry
    {
        public static ServiceCollection AddExRoute(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ExRouteTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseExRoute(this ServiceProvider serviceProvider)
        {
            ExRouteTransfer exRouteTransfer = serviceProvider.GetService<ExRouteTransfer>();
            return serviceProvider;
        }
    }
}
