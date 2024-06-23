using link.config;
using link.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.plugins.connections
{
    public sealed class ConnectionsStartup : IStartup
    {
        public string Name => "connections";

        public bool Required => true;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => new string[] { "relay","tunnel"};

        public StartupLoadType LoadType =>  StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ConnectionsApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
