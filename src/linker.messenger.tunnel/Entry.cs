using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.tunnel;
using Microsoft.Extensions.DependencyInjection;
using linker.messenger.signin.args;
using linker.messenger.sync;
using linker.libs.web;
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

            serviceCollection.AddSingleton<SignInArgsNet>();

            serviceCollection.AddSingleton<TunnelSyncTransports>();

            

            return serviceCollection;
        }
        public static ServiceProvider UseTunnelClient(this ServiceProvider serviceProvider)
        {
            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgsClient> { serviceProvider.GetService<SignInArgsNet>() });

            TunnelNetworkTransfer tunnelNetworkTransfer = serviceProvider.GetService<TunnelNetworkTransfer>();

            TunnelTransfer tunnelTransfer = serviceProvider.GetService<TunnelTransfer>();
            TunnelClientExcludeIPTransfer tunnelClientExcludeIPTransfer = serviceProvider.GetService<TunnelClientExcludeIPTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TunnelClientMessenger>() });


            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TunnelDecenter>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<TunnelApiController>() });


            ExRouteTransfer exRouteTransfer = serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<TunnelExRoute>() });


            SyncTreansfer syncTreansfer = serviceProvider.GetService<SyncTreansfer>();
            syncTreansfer.AddSyncs(new List<ISync> {
                serviceProvider.GetService<TunnelSyncTransports>(),
            });

            tunnelTransfer.RebuildTransports();

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
