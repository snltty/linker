using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.plugins.tunnel;
using linker.tunnel;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.tunnel
{
    public static class Entry
    {
        public static ServiceCollection AddTunnelClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelClientExcludeIPTransfer>();
            serviceCollection.AddSingleton<ITunnelMessengerAdapter, TunnelMessengerAdapter>();
            serviceCollection.AddSingleton<TunnelClientMessenger>();

            serviceCollection.AddSingleton<TunnelNetworkTransfer>();

            serviceCollection.AddSingleton<TunnelDecenter>();

            serviceCollection.AddSingleton<TunnelApiController>();

            serviceCollection.AddSingleton<TunnelExRoute>();

            return serviceCollection;
        }
        public static ServiceProvider UseTunnelClient(this ServiceProvider serviceProvider)
        {
            TunnelNetworkTransfer tunnelNetworkTransfer = serviceProvider.GetService<TunnelNetworkTransfer>();

            TunnelTransfer tunnelTransfer = serviceProvider.GetService<TunnelTransfer>();
            TunnelClientExcludeIPTransfer signInArgsTransfer = serviceProvider.GetService<TunnelClientExcludeIPTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TunnelClientMessenger>() });


            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TunnelDecenter>() });

            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<TunnelApiController>() });


            ExRouteTransfer exRouteTransfer= serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<TunnelExRoute>() });

            return serviceProvider;
        }


        public static ServiceCollection AddTunnelServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TunnelServerMessenger>();
            serviceCollection.AddSingleton<TunnelServerExternalResolver>();
            return serviceCollection;
        }
        public static ServiceProvider UseTunnelServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TunnelServerMessenger>() });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<TunnelServerExternalResolver>() });

            return serviceProvider;
        }
    }
}
