
using linker.libs.web;
using linker.messenger.api;
using Microsoft.Extensions.DependencyInjection;

namespace linker.messenger.plan
{
    public static class Entry
    {
        public static ServiceCollection AddPlanClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PlanTransfer>();
            serviceCollection.AddSingleton<PlanApiController>();
            serviceCollection.AddSingleton<PlanClientMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UsePlanClient(this ServiceProvider serviceProvider)
        {
            PlanTransfer planTransfer = serviceProvider.GetService<PlanTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PlanClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<PlanApiController>() });

            return serviceProvider;
        }
        public static ServiceCollection AddPlanServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<PlanServerMessenger>();

            return serviceCollection;
        } 
        public static ServiceProvider UsePlanServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<PlanServerMessenger>() });
            return serviceProvider;
        }

    }
}
