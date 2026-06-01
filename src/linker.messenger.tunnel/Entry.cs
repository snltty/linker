using linker.libs.web;
using linker.messenger.decenter;
using linker.messenger.rpolicy;
using linker.messenger.signin.args;
using linker.messenger.sync;
using linker.messenger.tunnel.client;
using linker.messenger.tunnel.server;
using linker.tunnel;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.tunnel
{
    public static class Entry
    {
        public static ServiceCollection AddTunnelClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelExclusionPolicyTransfer>();
            serviceCollection.AddSingleton<ITunnelMessengerAdapter, TunnelMessengerAdapter>();
            serviceCollection.AddSingleton<TunnelClientMessenger>();

            serviceCollection.AddSingleton<TunnelNetworkTransfer>();

            serviceCollection.AddSingleton<TunnelDecenter>();

            serviceCollection.AddSingleton<TunnelApiController>();

            serviceCollection.AddSingleton<TunnelRouteExclusionPolicy>();

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
            TunnelExclusionPolicyTransfer tunnelClientExcludeIPTransfer = serviceProvider.GetService<TunnelExclusionPolicyTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TunnelClientMessenger>() });


            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TunnelDecenter>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<TunnelApiController>() });


            RouteExclusionPolicyTransfer routeExclusionPolicyTransfer = serviceProvider.GetService<RouteExclusionPolicyTransfer>();
            routeExclusionPolicyTransfer.AddRouteExclusionPolicys(new List<IRouteExclusionPolicy> { serviceProvider.GetService<TunnelRouteExclusionPolicy>() });


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
            serviceCollection.AddSingleton<TunnelServerWanResolver>();
            return serviceCollection;
        }
        public static ServiceProvider UseTunnelServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TunnelServerMessenger>() });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<TunnelServerWanResolver>() });

            return serviceProvider;
        }
    }
}
