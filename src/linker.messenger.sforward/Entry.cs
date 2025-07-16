using linker.libs;
using linker.libs.web;
using linker.messenger.decenter;
using linker.messenger.plan;
using linker.messenger.sforward.client;
using linker.messenger.sforward.server;
using linker.messenger.sforward.server.validator;
using linker.plugins.sforward.messenger;
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

            serviceCollection.AddSingleton<SForwardDecenter>();


            serviceCollection.AddSingleton<SForwardProxy>();

            serviceCollection.AddSingleton<SForwardPlanHandle>();

            return serviceCollection;
        }
        public static ServiceProvider UseSForwardClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<SForwardApiController>() });

            SForwardClientTransfer sForwardClientTransfer = serviceProvider.GetService<SForwardClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SForwardClientMessenger>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<SForwardDecenter>() });


            PlanTransfer planTransfer = serviceProvider.GetService<PlanTransfer>();
            planTransfer.AddHandle(serviceProvider.GetService<SForwardPlanHandle>());
            return serviceProvider;
        }


        public static ServiceCollection AddSForwardServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<SForwardServerMessenger>();
            serviceCollection.AddSingleton<ISForwardServerCahing, SForwardServerCahing>();
            serviceCollection.AddSingleton<SForwardValidatorTransfer>();
            serviceCollection.AddSingleton<SForwardValidator>();

            serviceCollection.AddSingleton<SForwardProxy>();

            return serviceCollection;
        }
        public static ServiceProvider UseSForwardServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SForwardServerMessenger>() });

            SForwardValidatorTransfer sForwardValidatorTransfer = serviceProvider.GetService<SForwardValidatorTransfer>();
            sForwardValidatorTransfer.AddValidators(new List<ISForwardValidator> { serviceProvider.GetService<SForwardValidator>() });

            SForwardProxy sForwardProxy = serviceProvider.GetService<SForwardProxy>();
            ISForwardServerStore sForwardServerStore = serviceProvider.GetService<ISForwardServerStore>();
            if (sForwardServerStore.WebPort > 0)
            {
                sForwardProxy.Start(sForwardServerStore.WebPort, true, 3, "3494B7B2-1D9E-4DA2-B4F7-8C439EB03912");
                LoggerHelper.Instance.Debug($"start web forward in {sForwardServerStore.WebPort}");
            }

            return serviceProvider;
        }
    }
}
