using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.config
{
    public sealed class ConfigStartup : IStartup
    {
        public string Name => "config";

        public bool Required => true;

        public StartupLevel Level =>  StartupLevel.Normal;

        public string[] Dependent => Array.Empty<string>();

        public StartupLoadType LoadType =>  StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ConfigClientApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
        }
    }
}
