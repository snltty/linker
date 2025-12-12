using linker.libs;
using linker.libs.web;
using linker.messenger.action;
using linker.messenger.api;
using linker.messenger.firewall;
using linker.messenger.flow.history;
using linker.messenger.forward;
using linker.messenger.listen;
using linker.messenger.logger;
using linker.messenger.node;
using linker.messenger.pcp;
using linker.messenger.plan;
using linker.messenger.relay.client;
using linker.messenger.relay.server;
using linker.messenger.sforward.client;
using linker.messenger.sforward.server;
using linker.messenger.signin;
using linker.messenger.socks5;
using linker.messenger.store.file.action;
using linker.messenger.store.file.api;
using linker.messenger.store.file.common;
using linker.messenger.store.file.firewall;
using linker.messenger.store.file.flow;
using linker.messenger.store.file.forward;
using linker.messenger.store.file.logger;
using linker.messenger.store.file.messenger;
using linker.messenger.store.file.pcp;
using linker.messenger.store.file.plan;
using linker.messenger.store.file.relay;
using linker.messenger.store.file.server;
using linker.messenger.store.file.sforward;
using linker.messenger.store.file.signIn;
using linker.messenger.store.file.socks5;
using linker.messenger.store.file.tunnel;
using linker.messenger.store.file.tuntap;
using linker.messenger.store.file.updater;
using linker.messenger.store.file.wakeup;
using linker.messenger.store.file.wlist;
using linker.messenger.sync;
using linker.messenger.tunnel;
using linker.messenger.tuntap;
using linker.messenger.tuntap.lease;
using linker.messenger.updater;
using linker.messenger.wakeup;
using linker.messenger.wlist;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
namespace linker.messenger.store.file
{
    public static class Entry
    {
        public static ServiceCollection AddStoreFile(this ServiceCollection serviceCollection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info("add store file");

            serviceCollection.AddSingleton<Storefactory>();
            serviceCollection.AddSingleton<FileConfig>();
            serviceCollection.AddSingleton<RunningConfig>();

            serviceCollection.AddSingleton<ConfigApiController>();

            serviceCollection.AddSingleton<ICommonStore, CommonStore>();

            serviceCollection.AddSingleton<IApiStore, ApiStore>();
            serviceCollection.AddSingleton<IAccessStore, AccessStore>();

            serviceCollection.AddSingleton<IUpdaterCommonStore, UpdaterCommonStore>();
            serviceCollection.AddSingleton<IUpdaterClientStore, UpdaterClientStore>();
            serviceCollection.AddSingleton<IUpdaterServerStore, UpdaterServerStore>();

            serviceCollection.AddSingleton<IRelayClientStore, RelayClientStore>();
            serviceCollection.AddSingleton<IRelayNodeConfigStore, RelayServerConfigStore>();
            serviceCollection.AddSingleton<IRelayNodeStore, RelayServerNodeStore>();
            serviceCollection.AddSingleton<IRelayServerWhiteListStore, RelayNodeWhiteListStore>();
            serviceCollection.AddSingleton<IRelayServerMasterDenyStore, RelayServerMasterDenyStore>();


            serviceCollection.AddSingleton<ITunnelClientStore, TunnelClientStore>();

            serviceCollection.AddSingleton<ISignInClientStore, SignInClientStore>();
            serviceCollection.AddSingleton<ISignInServerStore, SignInServerStore>();
            serviceCollection.AddSingleton<SignInSyncSecretKey>();
            serviceCollection.AddSingleton<SignInSyncUserId>();
            serviceCollection.AddSingleton<SignInSyncServer>();

            serviceCollection.AddSingleton<SignInSyncGroupSecretKey>();


            serviceCollection.AddSingleton<IActionClientStore, ActionClientStore>();
            serviceCollection.AddSingleton<IActionServerStore, ActionServerStore>();

            serviceCollection.AddSingleton<IListenStore, ListenStore>();

            serviceCollection.AddSingleton<IMessengerStore, MessengerStore>();

            serviceCollection.AddSingleton<IPcpStore, PcpStore>();

            serviceCollection.AddSingleton<ISocks5Store, Socks5Store>();

            serviceCollection.AddSingleton<ISForwardClientStore, SForwardClientStore>();
            serviceCollection.AddSingleton<ISForwardNodeConfigStore, SForwardServerConfigStore>();
            serviceCollection.AddSingleton<ISForwardNodeStore, SForwardServerNodeStore>();
            serviceCollection.AddSingleton<ISForwardServerWhiteListStore, SForwardNodeWhiteListStore>();
            serviceCollection.AddSingleton<ISForwardServerMasterDenyStore, SForwardServerMasterDenyStore>();

            serviceCollection.AddSingleton<ILoggerStore, LoggerStore>();

            serviceCollection.AddSingleton<IForwardClientStore, ForwardClientStore>();

            serviceCollection.AddSingleton<ITuntapClientStore, TuntapClientStore>();
            serviceCollection.AddSingleton<ILeaseServerStore, LeaseServerStore>();
            serviceCollection.AddSingleton<ILeaseClientStore, LeaseClientStore>();

            serviceCollection.AddSingleton<IPlanStore, PlanStore>();


            serviceCollection.AddSingleton<ExportResolver>();


            serviceCollection.AddSingleton<IFirewallClientStore, FirewallClientStore>();

            serviceCollection.AddSingleton<IWakeupClientStore, WakeupClientStore>();

            serviceCollection.AddSingleton<IWhiteListServerStore, WhiteListServerStore>();

            serviceCollection.AddSingleton<IFlowHistoryStore, FlowHistoryStore>();


            return serviceCollection;
        }
        public static ServiceProvider UseStoreFile(this ServiceProvider serviceProvider, JsonDocument config = default)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info("use store file");

            FileConfig fileConfig = serviceProvider.GetService<FileConfig>();
            fileConfig.Save(config);
            RunningConfig runningConfig = serviceProvider.GetService<RunningConfig>();

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<ConfigApiController>() });

            SyncTreansfer syncTreansfer = serviceProvider.GetService<SyncTreansfer>();
            syncTreansfer.AddSyncs(new List<ISync> {
                serviceProvider.GetService<SignInSyncSecretKey>(),
                serviceProvider.GetService<SignInSyncUserId>(),
                serviceProvider.GetService<SignInSyncGroupSecretKey>(),
                serviceProvider.GetService<SignInSyncServer>(),

            });

            ResolverTransfer resolverTransfer = serviceProvider.GetService<ResolverTransfer>();
            resolverTransfer.AddResolvers(new List<IResolver>
            {
                serviceProvider.GetService<ExportResolver>(),
            });

            return serviceProvider;
        }
    }
}
