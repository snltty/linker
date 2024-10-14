using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.socks5
{
    /// <summary>
    /// socks5
    /// </summary>
    public sealed class Socks5Startup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "socks5";
        public bool Required => true;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<Socks5Resolver>();
        }


        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
