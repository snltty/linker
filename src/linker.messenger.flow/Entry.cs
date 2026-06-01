using linker.libs.web;
using linker.messenger.flow.history;
using linker.messenger.flow.messenger;
using linker.messenger.flow.webapi;
using linker.messenger.forward;
using linker.messenger.relay.server;
using linker.messenger.reverse.proxy;
using linker.messenger.socks5;
using linker.messenger.tunnel.server;
using linker.messenger.tuntap.client;
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

            serviceCollection.AddSingleton<FlowReverse>();
            serviceCollection.AddSingleton<ReverseProxy, FlowReverseProxy>();

            serviceCollection.AddSingleton<FlowForward>();
            serviceCollection.AddSingleton<ForwardProxy, FlowForwardProxy>();

            serviceCollection.AddSingleton<FlowSocks5>();
            serviceCollection.AddSingleton<Socks5Proxy, FlowSocks5Proxy>();


            serviceCollection.AddSingleton<FlowTunnel>();
            serviceCollection.AddSingleton<TuntapProxy, FlowTuntapProxy>();




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
            flowTransfer.AddFlows(new List<IFlow> {
                serviceProvider.GetService<FlowMessenger>(),
                serviceProvider.GetService<FlowReverse>(),
                serviceProvider.GetService<FlowForward>(),
                serviceProvider.GetService<FlowSocks5>(),
                serviceProvider.GetService<FlowTunnel>(),
            });

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
            serviceCollection.AddSingleton<TunnelServerWanResolver, ExternalResolverFlow>();

            serviceCollection.AddSingleton<FlowReverse>();
            serviceCollection.AddSingleton<ReverseProxy, FlowReverseProxy>();

            serviceCollection.AddSingleton<FlowHistoryTransfer>();


            serviceCollection.AddSingleton<WebApiOnlineController>();
            serviceCollection.AddSingleton<WebApiCitysController>();
            serviceCollection.AddSingleton<WebApiSystemsController>();


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
                serviceProvider.GetService<FlowReverse>(),
                serviceProvider.GetService<FlowResolver>(),
            });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<linker.messenger.flow.messenger.FlowMessenger>() });


            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver> { serviceProvider.GetService<FlowResolver>() });

            //FlowHistoryTransfer flowHistoryTransfer = serviceProvider.GetService<FlowHistoryTransfer>();
            IWebApiServer webApiServer = serviceProvider.GetService<IWebApiServer>();
            webApiServer.AddControllers(new List<IWebApiController> {
                serviceProvider.GetService<WebApiOnlineController>(),
                serviceProvider.GetService<WebApiCitysController>(),
                serviceProvider.GetService<WebApiSystemsController>(),
            });

            return serviceProvider;
        }
    }
}
