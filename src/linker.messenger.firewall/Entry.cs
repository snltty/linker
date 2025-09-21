
using linker.libs.web;
using linker.messenger.firewall.hooks;
using linker.messenger.forward.proxy;
using linker.messenger.socks5;
using linker.messenger.sync;
using linker.nat;
using linker.tun.hook;
using linker.tun;
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
            socks5Proxy.AddHooks(new List<ILinkerSocks5Hook> { serviceProvider.GetService<Socks5FirewallHook>() });

            ForwardProxy forwardProxy = serviceProvider.GetService<ForwardProxy>();
            forwardProxy.AddHooks(new List<ILinkerForwardHook> { serviceProvider.GetService<ForwardFirewallHook>() });

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
