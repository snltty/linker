using linker.client.config;
using linker.config;
using linker.plugins.config.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.config
{
    public sealed class ConfigStartup : IStartup
    {
        public string Name => "config";

        public bool Required => true;

        public StartupLevel Level => StartupLevel.Top;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<ConfigClientApiController>();

            serviceCollection.AddSingleton<ConfigClientMessenger>();

            serviceCollection.AddSingleton<RunningConfig>();


            serviceCollection.AddSingleton<AccessTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<ConfigServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            RunningConfig runningConfig = serviceProvider.GetService<RunningConfig>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
