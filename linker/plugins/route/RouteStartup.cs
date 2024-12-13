using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.route
{
    public sealed class RouteStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "route";

        public bool Required => false;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<RouteExcludeIPTransfer>();
            serviceCollection.AddSingleton<RouteExcludeIPTypesLoader>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            RouteExcludeIPTransfer excludeIPTransfer = serviceProvider.GetService<RouteExcludeIPTransfer>();
            RouteExcludeIPTypesLoader ruteExcludeIPTypesLoader = serviceProvider.GetService<RouteExcludeIPTypesLoader>();

        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {

        }
    }
}
