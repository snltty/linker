using linker.config;
using linker.plugins.access.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.access
{
    public sealed class AccessStartup : IStartup
    {
        public string Name => "access";

        public bool Required => true;

        public StartupLevel Level => StartupLevel.Top;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<AccessApiController>();

            serviceCollection.AddSingleton<AccessClientMessenger>();
            serviceCollection.AddSingleton<AccessTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<AccessServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
