using linker.libs.web;
using linker.messenger.sync;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.updater
{
    public static class Entry
    {
        public static ServiceCollection AddUpdaterClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<UpdaterApiController>();

            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterClientTransfer>();

            serviceCollection.AddSingleton<UpdaterClientMessenger>();

            serviceCollection.AddSingleton<UpdaterConfigSyncSecretKey>();

            serviceCollection.AddSingleton<IUpdaterInstaller, UpdaterInstaller>();

            return serviceCollection;
        }
        public static ServiceProvider UseUpdaterClient(this ServiceProvider serviceProvider)
        {
            UpdaterClientTransfer updaterClientTransfer = serviceProvider.GetService<UpdaterClientTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<UpdaterClientMessenger>() });

            SyncTreansfer syncTransfer = serviceProvider.GetService<SyncTreansfer>();
            syncTransfer.AddSyncs(new List<ISync> { serviceProvider.GetService<UpdaterConfigSyncSecretKey>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<UpdaterApiController>() });

            return serviceProvider;
        }


        public static ServiceCollection AddUpdaterServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterServerTransfer>();
            serviceCollection.AddSingleton<UpdaterServerMessenger>();

            serviceCollection.AddSingleton<IUpdaterInstaller, UpdaterInstaller>();

            return serviceCollection;
        }
        public static ServiceProvider UseUpdaterServer(this ServiceProvider serviceProvider)
        {
            UpdaterServerTransfer updaterServerTransfer = serviceProvider.GetService<UpdaterServerTransfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<UpdaterServerMessenger>() });

            return serviceProvider;
        }
    }
}
