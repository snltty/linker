using linker.messenger.access;
using linker.messenger.action;
using linker.messenger.api;
using linker.messenger.listen;
using linker.messenger.pcp;
using linker.messenger.relay.client;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.messenger.socks5;
using linker.messenger.store.file.action;
using linker.messenger.store.file.api;
using linker.messenger.store.file.messenger;
using linker.messenger.store.file.pcp;
using linker.messenger.store.file.relay;
using linker.messenger.store.file.server;
using linker.messenger.store.file.signIn;
using linker.messenger.store.file.socks5;
using linker.messenger.store.file.tunnel;
using linker.messenger.store.file.updater;
using linker.messenger.sync;
using linker.messenger.updater;
using linker.plugins.tunnel;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.store.file
{
    public static class Entry
    {
        public static ServiceCollection AddStoreFile(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ConfigApiController>();

            serviceCollection.AddSingleton<IApiStore, ApiStore>();
            serviceCollection.AddSingleton<IAccessStore, AccessStore>();

            serviceCollection.AddSingleton<IUpdaterCommonStore, UpdaterCommonStore>();
            serviceCollection.AddSingleton<IUpdaterClientStore, UpdaterClientStore>();
            serviceCollection.AddSingleton<IUpdaterServerStore, UpdaterServerStore>();

            serviceCollection.AddSingleton<IRelayClientStore, RelayClientStore>();
            serviceCollection.AddSingleton<IRelayServerStore, RelayServerStore>();
            serviceCollection.AddSingleton<IRelayServerNodeStore, RelayServerNodeStore>();
            serviceCollection.AddSingleton<IRelayServerMasterStore, RelayServerMasterStore>();


            serviceCollection.AddSingleton<ITunnelClientStore, TunnelClientStore>();

            serviceCollection.AddSingleton<ISignInClientStore, SignInClientStore>();
            serviceCollection.AddSingleton<ISignInServerStore, SignInServerStore>();
            serviceCollection.AddSingleton<SignInSyncSecretKey>();
            serviceCollection.AddSingleton<SignInSyncGroupSecretKey>();


            serviceCollection.AddSingleton<IActionStore, ActionStore>();

            serviceCollection.AddSingleton<IListenStore, ListenStore>();

            serviceCollection.AddSingleton<IMessengerStore, MessengerStore>();

            serviceCollection.AddSingleton<IPcpStore, PcpStore>();

            serviceCollection.AddSingleton<ISocks5Store, Socks5Store>();

            return serviceCollection;
        }
        public static ServiceProvider UseStoreFile(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<ConfigApiController>() });

            SyncTreansfer syncTreansfer = serviceProvider.GetService<SyncTreansfer>();
            syncTreansfer.AddSyncs(new List<ISync> {
                serviceProvider.GetService<SignInSyncSecretKey>(),
                serviceProvider.GetService<SignInSyncGroupSecretKey>(),
            });

            return serviceProvider;
        }
    }
}
