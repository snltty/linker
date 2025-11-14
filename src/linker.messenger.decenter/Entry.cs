using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.decenter
{
    public static class Entry
    {
        public static ServiceCollection AddDecenterClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DecenterClientTransfer>();

            serviceCollection.AddSingleton<DecenterClientMessenger>();

            serviceCollection.AddSingleton<CounterDecenter>();
            serviceCollection.AddSingleton<DecenterApiController>();

            return serviceCollection;
        }
        public static ServiceProvider UseDecenterClient(this ServiceProvider serviceProvider)
        {
            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<CounterDecenter>() });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<DecenterClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<DecenterApiController>() });

            return serviceProvider;
        }

        public static ServiceCollection AddDecenterServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DecenterServerMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UseDecenterServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<DecenterServerMessenger>() });

            return serviceProvider;
        }
    }
}
