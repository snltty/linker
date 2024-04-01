using cmonitor.config;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.startup
{
    public interface IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies);
        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies);
    }
}
