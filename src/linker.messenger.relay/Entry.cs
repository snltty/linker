using linker.libs.web;
using linker.messenger.node;
using linker.messenger.relay.client;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.relay.server.validator;
using linker.messenger.relay.transport;
using linker.messenger.relay.webapi;
using linker.messenger.sync;
using linker.tunnel;
using linker.tunnel.transport;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.relay
{
    public static class Entry
    {
        public static ServiceCollection AddRelayClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TransportRelay>();
            serviceCollection.AddSingleton<TunnelWanPortProtocolRelay>();

            serviceCollection.AddSingleton<RelayClientMessenger>();

            serviceCollection.AddSingleton<RelaySyncDefault>();

            serviceCollection.AddSingleton<RelayApiController>();

            serviceCollection.AddSingleton<RelayClientTestTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseRelayClient(this ServiceProvider serviceProvider)
        {
            TunnelTransfer tunnelTransfer = serviceProvider.GetService<TunnelTransfer>();
            tunnelTransfer.AddTransport(serviceProvider.GetService<TransportRelay>());
            tunnelTransfer.AddProtocol(serviceProvider.GetService<TunnelWanPortProtocolRelay>());



            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<RelayClientMessenger>() });

            SyncTreansfer syncTreansfer = serviceProvider.GetService<SyncTreansfer>();
            syncTreansfer.AddSyncs(new List<ISync> { serviceProvider.GetService<RelaySyncDefault>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<RelayApiController>() });

            RelayClientTestTransfer relayClientTestTransfer = serviceProvider.GetService<RelayClientTestTransfer>();

            return serviceProvider;
        }


        public static ServiceCollection AddRelayServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<RelayServerMessenger>();
            serviceCollection.AddSingleton<RelayServerNodeTransfer>();
            serviceCollection.AddSingleton<RelayServerNodeReportTransfer>();
            
            serviceCollection.AddSingleton<RelayServerMasterTransfer>();
            serviceCollection.AddSingleton<RelayServerReportResolver>();
            
            serviceCollection.AddSingleton<RelayServerResolver>();
            serviceCollection.AddSingleton<NodeConnectionResolver>();
            serviceCollection.AddSingleton<RelayServerConnectionTransfer>();

            serviceCollection.AddSingleton<IRelayServerCaching, RelayServerCachingMemory>();

            serviceCollection.AddSingleton<RelayServerValidatorTransfer>();

            serviceCollection.AddSingleton<IRelayServerWhiteListStore, RelayServerWhiteListStore>();


            serviceCollection.AddSingleton<WebApiRelayNodesController>();
            return serviceCollection;
        }
        public static ServiceProvider UseRelayServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<RelayServerMessenger>() });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver>
            {
                serviceProvider.GetService<RelayServerReportResolver>(),
                serviceProvider.GetService<RelayServerResolver>(),
                serviceProvider.GetService<NodeConnectionResolver>(),
            });

            RelayServerNodeTransfer relayServerNodeTransfer = serviceProvider.GetService<RelayServerNodeTransfer>();
            RelayServerMasterTransfer relayServerMasterTransfer = serviceProvider.GetService<RelayServerMasterTransfer>();

            IWebApiServer webApiServer = serviceProvider.GetService<IWebApiServer>();
            webApiServer.AddController(serviceProvider.GetService<WebApiRelayNodesController>());

            return serviceProvider;
        }
    }
}
