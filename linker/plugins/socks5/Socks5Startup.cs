using linker.config;
using linker.plugins.socks5;
using linker.plugins.socks5.messenger;
using linker.startup;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.Socks5
{
    public sealed class Socks5Startup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "socks5";
        public bool Required => false;
        public string[] Dependent => new string[] { "messenger", "relay", "tunnel", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<Socks5ClientApiController>();
            serviceCollection.AddSingleton<LinkerTunDeviceAdapter>();
            serviceCollection.AddSingleton<TunnelProxy>();

            serviceCollection.AddSingleton<Socks5ClientMessenger>();

            serviceCollection.AddSingleton<Socks5ConfigTransfer>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<Socks5ServerMessenger>();

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            TunnelProxy socks5Proxy = serviceProvider.GetService<TunnelProxy>();

            Socks5ConfigTransfer socks5ConfigTransfer = serviceProvider.GetService<Socks5ConfigTransfer>();

        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
