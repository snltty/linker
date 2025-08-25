using linker.libs.web;
using linker.messenger.relay.server;
using linker.messenger.sforward.server;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.cdkey
{
    public static class Entry
    {
        public static ServiceCollection AddCdkeyClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CdkeyApiController>();

            return serviceCollection;
        }
        public static ServiceProvider UseCdkeyClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<CdkeyApiController>() });

            return serviceProvider;
        }


        public static ServiceCollection AddCdkeyServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CdkeyServerMessenger>();

            serviceCollection.AddSingleton<IRelayServerCdkeyStore, RelayCdkeyStore>();
            serviceCollection.AddSingleton<ISForwardServerCdkeyStore, SForwardCdkeyStore>();

            return serviceCollection;
        }
        public static ServiceProvider UseCdkeyServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<CdkeyServerMessenger>() });

            return serviceProvider;
        }
    }
}
