using cmonitor.config;
using cmonitor.plugins.modes.db;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.modes
{
    public sealed class ModesStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "modes";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ModesApiController>();
            serviceCollection.AddSingleton<IModesDB, ModesDB>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
