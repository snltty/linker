using linker.libs.web;
using linker.messenger.sync;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.cdkey
{
    public static class Entry
    {
        public static ServiceCollection AddCdkeyClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CdkeyApiController>();

            serviceCollection.AddSingleton<CdkeyConfigSyncSecretKey>();

            return serviceCollection;
        }
        public static ServiceProvider UseCdkeyClient(this ServiceProvider serviceProvider)
        {
            SyncTreansfer syncTransfer = serviceProvider.GetService<SyncTreansfer>();
            syncTransfer.AddSyncs(new List<ISync> { serviceProvider.GetService<CdkeyConfigSyncSecretKey>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<CdkeyApiController>() });

            return serviceProvider;
        }


        public static ServiceCollection AddCdkeyServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CdkeyServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseCdkeyServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<CdkeyServerMessenger>() });

            return serviceProvider;
        }
    }
}
