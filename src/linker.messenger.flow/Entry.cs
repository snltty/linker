using linker.libs.web;
using linker.messenger.api;
using linker.messenger.flow.history;
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

            serviceCollection.AddSingleton<FlowMessenger>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolverFlow>();
            serviceCollection.AddSingleton<IMessengerSender, MessengerSenderFlow>();

            return serviceCollection;
        }
        public static ServiceProvider UseFlowClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<FlowApiController>() });

            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
            flowTransfer.AddFlows(new List<IFlow> { serviceProvider.GetService<FlowMessenger>() });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FlowClientMessenger>() });

            return serviceProvider;
        }


        public static ServiceCollection AddFlowServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<linker.messenger.flow.messenger.FlowMessenger>();
            serviceCollection.AddSingleton<FlowTransfer>();
            serviceCollection.AddSingleton<FlowResolver>();

            serviceCollection.AddSingleton<FlowMessenger>();
            serviceCollection.AddSingleton<IMessengerResolver, MessengerResolverFlow>();
            serviceCollection.AddSingleton<IMessengerSender, MessengerSenderFlow>();

            serviceCollection.AddSingleton<FlowRelay>();
            serviceCollection.AddSingleton<RelayServerResolver, RelayResolverFlow>();
            serviceCollection.AddSingleton<RelayReportFlow>();
            serviceCollection.AddSingleton<RelayServerReportResolver, RelayReportResolverFlow>();

            serviceCollection.AddSingleton<FlowExternal>();
            serviceCollection.AddSingleton<TunnelServerExternalResolver, ExternalResolverFlow>();

            serviceCollection.AddSingleton<FlowSForward>();
            serviceCollection.AddSingleton<SForwardProxy, SForwardProxyFlow>();

            serviceCollection.AddSingleton<FlowHistoryTransfer>();


            return serviceCollection;
        }
        public static ServiceProvider UseFlowServer(this ServiceProvider serviceProvider)
        {

            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
            flowTransfer.AddFlows(new List<IFlow> {
                serviceProvider.GetService<FlowMessenger>(),
                serviceProvider.GetService<FlowRelay>(),
                serviceProvider.GetService<RelayReportFlow>(),
                serviceProvider.GetService<FlowExternal>(),
                serviceProvider.GetService<FlowSForward>(),
                serviceProvider.GetService<FlowResolver>(),
            });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<linker.messenger.flow.messenger.FlowMessenger>() });


            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<FlowResolver>() });

            //FlowHistoryTransfer flowHistoryTransfer = serviceProvider.GetService<FlowHistoryTransfer>();

            return serviceProvider;
        }
    }
}
