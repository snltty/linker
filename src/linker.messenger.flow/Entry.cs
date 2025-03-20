using linker.messenger.api;
using linker.messenger.flow.messenger;
using linker.messenger.relay.server;
using linker.messenger.tunnel;
using linker.plugins.sforward.proxy;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.flow
{
    public static class Entry
    {
        public static ServiceCollection AddFlowClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<FlowClientMessenger>();
            serviceCollection.AddSingleton<FlowApiController>();
            serviceCollection.AddSingleton<FlowTransfer>();

            serviceCollection.AddSingleton<MessengerFlow>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolverFlow>();
            serviceCollection.AddSingleton<IMessengerSender, MessengerSenderFlow>();

            return serviceCollection;
        }
        public static ServiceProvider UseFlowClient(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<FlowApiController>() });

            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
            flowTransfer.AddFlows(new List<IFlow> { serviceProvider.GetService<MessengerFlow>() });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FlowClientMessenger>() });

            return serviceProvider;
        }


        public static ServiceCollection AddFlowServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<FlowMessenger>();
            serviceCollection.AddSingleton<FlowTransfer>();
            serviceCollection.AddSingleton<FlowResolver>();

            serviceCollection.AddSingleton<MessengerFlow>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolverFlow>();
            serviceCollection.AddSingleton<IMessengerSender, MessengerSenderFlow>();

            serviceCollection.AddSingleton<RelayFlow>();
            serviceCollection.AddSingleton<RelayServerResolver, RelayResolverFlow>();
            serviceCollection.AddSingleton<RelayReportFlow>();
            serviceCollection.AddSingleton<RelayServerReportResolver, RelayReportResolverFlow>();

            serviceCollection.AddSingleton<ExternalFlow>();
            serviceCollection.AddSingleton<TunnelServerExternalResolver, ExternalResolverFlow>();

            serviceCollection.AddSingleton<SForwardFlow>();
            serviceCollection.AddSingleton<SForwardProxy, SForwardProxyFlow>();
            return serviceCollection;
        }
        public static ServiceProvider UseFlowServer(this ServiceProvider serviceProvider)
        {

            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
            flowTransfer.AddFlows(new List<IFlow> {
                serviceProvider.GetService<MessengerFlow>(),
                serviceProvider.GetService<RelayFlow>(),
                serviceProvider.GetService<RelayReportFlow>(),
                serviceProvider.GetService<ExternalFlow>(),
                serviceProvider.GetService<SForwardFlow>(),
                serviceProvider.GetService<FlowResolver>(),
            });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FlowMessenger>() });


            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<FlowResolver>() });

            return serviceProvider;
        }
    }
}
