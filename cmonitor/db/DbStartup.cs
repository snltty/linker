using cmonitor.config;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace cmonitor.db
{
    public sealed class DbStartupStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "db";
        public bool Required => false;
        public string[] Dependent => Array.Empty<string>();
        public StartupLoadType LoadType => StartupLoadType.Normal;

        bool loaded = false;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            Add(serviceCollection, config, assemblies);
        }

        private void Add(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                loaded = true;
                serviceCollection.AddSingleton<DBfactory>();
            }
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
