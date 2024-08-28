using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.net
{
    public sealed class NetStartup : IStartup
    {
        public string Name => "net";

        public bool Required => true;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<NetClientApiController>();
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
