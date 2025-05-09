
using linker.messenger.api;
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

            return serviceCollection;
        }
        public static ServiceProvider UseFirewallClient(this ServiceProvider serviceProvider)
        {
            LinkerFirewall linkerFirewall = serviceProvider.GetService<LinkerFirewall>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<FirewallClientMessenger>() });

            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<FirewallApiController>() });

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
