using linker.config;
using linker.plugins.decenter.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.decenter
{
    public sealed class DecenterStartup : IStartup
    {
        public string Name => "decenter";

        public bool Required => true;

        public StartupLevel Level => StartupLevel.Top;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<DecenterTransfer>();
            serviceCollection.AddSingleton<DecenterTypesLoader>();

            serviceCollection.AddSingleton<DecenterClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<DecenterServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            DecenterTransfer decenterTransfer = serviceProvider.GetService<DecenterTransfer>();
            DecenterTypesLoader decenterTypesLoader = serviceProvider.GetService<DecenterTypesLoader>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
