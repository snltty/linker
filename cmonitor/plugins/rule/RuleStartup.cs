using cmonitor.config;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.rule
{
    public sealed class RuleStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RuleApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
