using linker.libs;
using linker.libs.web;
using linker.messenger.node;
using linker.messenger.plan;
using linker.messenger.reverse.client;
using linker.messenger.reverse.messenger;
using linker.messenger.reverse.proxy;
using linker.messenger.reverse.server;
using linker.messenger.reverse.server.validator;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.reverse
{
    public static class Entry
    {
        public static ServiceCollection AddReverseClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ReverseApiController>();

            serviceCollection.AddSingleton<ReverseClientTransfer>();

            serviceCollection.AddSingleton<ReverseClientMessenger>();

            serviceCollection.AddSingleton<ReverseProxy>();

            serviceCollection.AddSingleton<ReversePlanHandle>();

            serviceCollection.AddSingleton<ReverseClientTestTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseReverseClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<ReverseApiController>() });

            ReverseClientTransfer ReverseClientTransfer = serviceProvider.GetService<ReverseClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ReverseClientMessenger>() });

            PlanTransfer planTransfer = serviceProvider.GetService<PlanTransfer>();
            planTransfer.AddHandle(serviceProvider.GetService<ReversePlanHandle>());


            serviceProvider.GetService<ReverseClientTestTransfer>();

            return serviceProvider;
        }


        public static ServiceCollection AddReverseServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ReverseServerMessenger>();
            serviceCollection.AddSingleton<IReverseServerCahing, ReverseServerCahing>();
            serviceCollection.AddSingleton<ReverseValidatorTransfer>();
            serviceCollection.AddSingleton<ReverseValidator>();

            serviceCollection.AddSingleton<ReverseProxy>();

            serviceCollection.AddSingleton<ReverseServerMasterTransfer>();
            serviceCollection.AddSingleton<ReverseServerNodeTransfer>();
            serviceCollection.AddSingleton<ReverseServerReportResolver>();

            serviceCollection.AddSingleton<NodeConnectionResolver>();
            serviceCollection.AddSingleton<ReverseServerConnectionTransfer>();

            serviceCollection.AddSingleton<ReverseServerNodeReportTransfer>();

            serviceCollection.AddSingleton<IReverseServerWhiteListStore, ReverseServerWhiteListStore>();

            return serviceCollection;
        }
        public static ServiceProvider UseReverseServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ReverseServerMessenger>() });

            ReverseValidatorTransfer ReverseValidatorTransfer = serviceProvider.GetService<ReverseValidatorTransfer>();
            ReverseValidatorTransfer.AddValidators(new List<IReverseValidator> { serviceProvider.GetService<ReverseValidator>() });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver>
            {
                serviceProvider.GetService<ReverseServerReportResolver>(),
                serviceProvider.GetService<NodeConnectionResolver>(),
            });

            serviceProvider.GetService<ReverseServerNodeTransfer>();
            serviceProvider.GetService<ReverseServerMasterTransfer>();

            ReverseProxy ReverseProxy = serviceProvider.GetService<ReverseProxy>();
            IReverseNodeConfigStore ReverseServerStore = serviceProvider.GetService<IReverseNodeConfigStore>();
            if (ReverseServerStore.Config.WebPort > 0)
            {
                ReverseProxy.StartHttp(ReverseServerStore.Config.WebPort, 3 );
                LoggerHelper.Instance.Debug($"start web forward in {ReverseServerStore.Config.WebPort}");
            }

            return serviceProvider;
        }
    }
}
