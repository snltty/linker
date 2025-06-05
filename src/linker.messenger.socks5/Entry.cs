using linker.libs.web;
using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.exroute;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.socks5
{
    public static class Entry
    {
        public static ServiceCollection AddSocks5Client(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Socks5ApiController>();
            serviceCollection.AddSingleton<TunnelProxy>();

            serviceCollection.AddSingleton<Socks5ClientMessenger>();

            serviceCollection.AddSingleton<Socks5Transfer>();

            serviceCollection.AddSingleton<Socks5Decenter>();
            serviceCollection.AddSingleton<Socks5ExRoute>();

            return serviceCollection;
        }
        public static ServiceProvider UseSocks5Client(this ServiceProvider serviceProvider)
        {
            TunnelProxy socks5Proxy = serviceProvider.GetService<TunnelProxy>();
            Socks5Transfer socks5Transfer = serviceProvider.GetService<Socks5Transfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<Socks5ClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<Socks5ApiController>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<Socks5Decenter>() });

            ExRouteTransfer exRouteTransfer = serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<Socks5ExRoute>() });

            return serviceProvider;
        }


        public static ServiceCollection AddSocks5Server(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Socks5ServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseSocks5Server(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<Socks5ServerMessenger>() });

            return serviceProvider;
        }
    }
}
