
using linker.libs.web;
using linker.messenger.sync;
using linker.snat;
using Microsoft.Extensions.DependencyInjection;

namespace linker.messenger.firewall
{
    public static class Entry
    {
        public static ServiceCollection AddFirewallClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<LinkerFirewall>();
            serviceCollection.AddSingleton<FirewallClientMessenger>();
            serviceCollection.AddSingleton<FirewallTransfer>();
            serviceCollection.AddSingleton<FirewallApiController>();
            serviceCollection.AddSingleton<FirewallSync>();

            return serviceCollection;
        }
        public static ServiceProvider UseFirewallClient(this ServiceProvider serviceProvider)
        {
            LinkerFirewall linkerFirewall = serviceProvider.GetService<LinkerFirewall>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FirewallClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<FirewallApiController>() });

            SyncTreansfer syncTransfer = serviceProvider.GetService<SyncTreansfer>();
            syncTransfer.AddSyncs(new List<ISync> { serviceProvider.GetService<FirewallSync>() });

            return serviceProvider;
        }


        public static ServiceCollection AddFirewallServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<FirewallServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseFirewallServer(this ServiceProvider serviceProvider)
        {

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FirewallServerMessenger>() });
            return serviceProvider;
        }
    }
}
