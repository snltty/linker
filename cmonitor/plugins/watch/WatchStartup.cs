using cmonitor.config;
using cmonitor.plugins.watch.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.watch
{
    public sealed class WatchStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<WatchReport>();

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            serviceProvider.GetService<WatchReport>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
