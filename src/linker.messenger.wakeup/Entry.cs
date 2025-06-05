
using linker.libs.web;
using linker.messenger.api;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.wakeup
{
    public static class Entry
    {

        public static ServiceCollection AddWakeupClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<WakeupClientMessenger>();
            serviceCollection.AddSingleton<WakeupApiController>();

            serviceCollection.AddSingleton<WakeupTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseWakeupClient(this ServiceProvider serviceProvider)
        {

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<WakeupClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<WakeupApiController>() });

            return serviceProvider;
        }
        public static ServiceCollection AddWakeupServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<WakeupServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseWakeupServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<WakeupServerMessenger>() });

            return serviceProvider;
        }
    }
}
