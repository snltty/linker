using cmonitor.config;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.devices
{
    public sealed class DevicesStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "devices";
        public bool Required => false;
        public string[] Dependent => Array.Empty<string>();
        public StartupLoadType LoadType =>  StartupLoadType.Dependent;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<DevicesApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
