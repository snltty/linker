using linker.libs.web;
using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.forward.proxy;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.forward
{
    public static class Entry
    {
        public static ServiceCollection AddForwardClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ForwardApiController>();

            serviceCollection.AddSingleton<ForwardTransfer>();

            serviceCollection.AddSingleton<ForwardClientMessenger>();

            serviceCollection.AddSingleton<ForwardProxy>();

            return serviceCollection;
        }
        public static ServiceProvider UseForwardClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<ForwardApiController>() });

            ForwardTransfer forwardTransfer = serviceProvider.GetService<ForwardTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ForwardClientMessenger>() });

            ForwardProxy forwardProxy= serviceProvider.GetService<ForwardProxy>();

            return serviceProvider;
        }


        public static ServiceCollection AddForwardServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ForwardServerMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UseForwardServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ForwardServerMessenger>() });

            return serviceProvider;
        }
    }
}
