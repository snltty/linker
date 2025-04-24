using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.messenger.tunnel;
using linker.messenger.tuntap.lease;
using linker.messenger.tuntap.messenger;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.tuntap
{
    public static class Entry
    {
        public static ServiceCollection AddTuntapClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TuntapApiController>();
            serviceCollection.AddSingleton<LinkerTunDeviceAdapter>();
            serviceCollection.AddSingleton<TuntapTransfer>();
            serviceCollection.AddSingleton<TuntapProxy>();

            serviceCollection.AddSingleton<TuntapClientMessenger>();
            serviceCollection.AddSingleton<LeaseClientTreansfer>();

            serviceCollection.AddSingleton<TuntapTunnelExcludeIP>();

            serviceCollection.AddSingleton<TuntapConfigTransfer>();
            serviceCollection.AddSingleton<TuntapPingTransfer>();

            serviceCollection.AddSingleton<TuntapDecenter>();

            serviceCollection.AddSingleton<TuntapAdapter>();

            serviceCollection.AddSingleton<TuntapExRoute>();

            serviceCollection.AddSingleton<ISystemInformation, SystemInformation>();

            return serviceCollection;
        }
        public static ServiceProvider UseTuntapClient(this ServiceProvider serviceProvider)
        {
            TuntapProxy tuntapProxy = serviceProvider.GetService<TuntapProxy>();
            TuntapTransfer tuntapTransfer = serviceProvider.GetService<TuntapTransfer>();

            LeaseClientTreansfer leaseTreansfer = serviceProvider.GetService<LeaseClientTreansfer>();

            TuntapConfigTransfer tuntapConfigTransfer = serviceProvider.GetService<TuntapConfigTransfer>();
            TuntapPingTransfer tuntapPingTransfer = serviceProvider.GetService<TuntapPingTransfer>();

            TuntapAdapter tuntapAdapter = serviceProvider.GetService<TuntapAdapter>();


            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TuntapClientMessenger>() });

            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<TuntapApiController>() });

            ExRouteTransfer exRouteTransfer= serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<TuntapExRoute>() });

            TunnelClientExcludeIPTransfer tunnelClientExcludeIPTransfer = serviceProvider.GetService<TunnelClientExcludeIPTransfer>();
            tunnelClientExcludeIPTransfer.AddTunnelExcludeIPs(new List<ITunnelClientExcludeIP> { serviceProvider.GetService<TuntapTunnelExcludeIP>() });

            DecenterClientTransfer decenterClientTransfer= serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TuntapDecenter>() });

            return serviceProvider;
        }


        public static ServiceCollection AddTuntapServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TuntapServerMessenger>();
            serviceCollection.AddSingleton<LeaseServerTreansfer>();

            serviceCollection.AddSingleton<ISystemInformation, SystemInformation>();

            return serviceCollection;
        }
        public static ServiceProvider UseTuntapServer(this ServiceProvider serviceProvider)
        {
            LeaseServerTreansfer leaseTreansfer = serviceProvider.GetService<LeaseServerTreansfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TuntapServerMessenger>() });

            return serviceProvider;
        }
    }
}
