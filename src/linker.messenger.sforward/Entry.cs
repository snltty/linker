using linker.libs;
using linker.libs.web;
using linker.messenger.decenter;
using linker.messenger.plan;
using linker.messenger.sforward.client;
using linker.messenger.sforward.messenger;
using linker.messenger.sforward.server;
using linker.messenger.sforward.server.validator;
using linker.plugins.sforward.proxy;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.sforward
{
    public static class Entry
    {
        public static ServiceCollection AddSForwardClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<SForwardApiController>();

            serviceCollection.AddSingleton<SForwardClientTransfer>();

            serviceCollection.AddSingleton<SForwardClientMessenger>();

            serviceCollection.AddSingleton<SForwardProxy>();

            serviceCollection.AddSingleton<SForwardPlanHandle>();

            serviceCollection.AddSingleton<SForwardClientTestTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseSForwardClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<SForwardApiController>() });

            SForwardClientTransfer sForwardClientTransfer = serviceProvider.GetService<SForwardClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SForwardClientMessenger>() });

            PlanTransfer planTransfer = serviceProvider.GetService<PlanTransfer>();
            planTransfer.AddHandle(serviceProvider.GetService<SForwardPlanHandle>());


            serviceProvider.GetService<SForwardClientTestTransfer>();

            return serviceProvider;
        }


        public static ServiceCollection AddSForwardServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<SForwardServerMessenger>();
            serviceCollection.AddSingleton<ISForwardServerCahing, SForwardServerCahing>();
            serviceCollection.AddSingleton<SForwardValidatorTransfer>();
            serviceCollection.AddSingleton<SForwardValidator>();

            serviceCollection.AddSingleton<SForwardProxy>();

            serviceCollection.AddSingleton<SForwardServerMasterTransfer>();
            serviceCollection.AddSingleton<SForwardServerNodeTransfer>();
            serviceCollection.AddSingleton<SForwardServerReportResolver>();

            serviceCollection.AddSingleton<ISForwardServerWhiteListStore, SForwardServerWhiteListStore>();

            return serviceCollection;
        }
        public static ServiceProvider UseSForwardServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SForwardServerMessenger>() });

            SForwardValidatorTransfer sForwardValidatorTransfer = serviceProvider.GetService<SForwardValidatorTransfer>();
            sForwardValidatorTransfer.AddValidators(new List<ISForwardValidator> { serviceProvider.GetService<SForwardValidator>() });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver>
            {
                serviceProvider.GetService<SForwardServerReportResolver>()
            });

            SForwardProxy sForwardProxy = serviceProvider.GetService<SForwardProxy>();
            ISForwardServerStore sForwardServerStore = serviceProvider.GetService<ISForwardServerStore>();
            if (sForwardServerStore.WebPort > 0)
            {
                sForwardProxy.StartHttp(sForwardServerStore.WebPort,3 );
                LoggerHelper.Instance.Debug($"start web forward in {sForwardServerStore.WebPort}");
            }

            return serviceProvider;
        }
    }
}
