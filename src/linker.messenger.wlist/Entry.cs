using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.wlist
{
    public static class Entry
    {
        public static ServiceCollection AddWhiteListClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<WhiteListApiController>();

            return serviceCollection;
        }
        public static ServiceProvider UseWhiteListClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<WhiteListApiController>() });

            return serviceProvider;
        }


        public static ServiceCollection AddWhiteListServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<WhiteListServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseWhiteListServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<WhiteListServerMessenger>() });

            return serviceProvider;
        }
    }
}
