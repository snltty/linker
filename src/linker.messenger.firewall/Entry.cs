
using linker.libs.web;
using linker.messenger.firewall.hooks;
using linker.messenger.socks5;
using linker.messenger.sync;
using linker.nat;
using linker.tun.hook;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;
using linker.messenger.forward;
using linker.forward;

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


            serviceCollection.AddSingleton<TuntapFirewallHook>();
            serviceCollection.AddSingleton<Socks5FirewallHook>();
            serviceCollection.AddSingleton<ForwardFirewallHook>();



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


            LinkerTunDeviceAdapter linkerTunDeviceAdapter = serviceProvider.GetService<LinkerTunDeviceAdapter>();
            linkerTunDeviceAdapter.AddHooks(new List<ILinkerTunPacketHook> { serviceProvider.GetService<TuntapFirewallHook>() });

            Socks5Proxy socks5Proxy = serviceProvider.GetService<Socks5Proxy>();
            socks5Proxy.AddHooks(new List<IForwardHook> { serviceProvider.GetService<Socks5FirewallHook>() });

            forward.ForwardProxy forwardProxy = serviceProvider.GetService<forward.ForwardProxy>();
            forwardProxy.AddHooks(new List<IForwardHook> { serviceProvider.GetService<ForwardFirewallHook>() });

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
