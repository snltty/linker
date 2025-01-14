using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.sforward.client;
using linker.messenger.sforward.server;
using linker.messenger.sforward.server.validator;
using linker.messenger.sync;
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

            serviceCollection.AddSingleton<SForwardSyncSecretKey>();

            serviceCollection.AddSingleton<SForwardDecenter>();


            serviceCollection.AddSingleton<SForwardProxy>();

            return serviceCollection;
        }
        public static ServiceProvider UseSForwardClient(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<SForwardApiController>() });

            SForwardClientTransfer sForwardClientTransfer = serviceProvider.GetService<SForwardClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SForwardClientMessenger>() });

            SyncTreansfer syncTransfer = serviceProvider.GetService<SyncTreansfer>();
            syncTransfer.AddSyncs(new List<ISync> { serviceProvider.GetService<SForwardSyncSecretKey>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<SForwardDecenter>() });

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

            return serviceProvider;
        }
    }
}
