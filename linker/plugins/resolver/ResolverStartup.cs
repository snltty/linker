using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.resolver
{
    /// <summary>
    /// 服务端插件
    /// </summary>
    public sealed class ResolverStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "resolver";
        public bool Required => true;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {

            serviceCollection.AddSingleton<ResolverTransfer>();
            serviceCollection.AddSingleton<ResolverTypesLoader>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<ResolverTransfer>();
            serviceCollection.AddSingleton<ResolverTypesLoader>();

        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;
                ResolverTypesLoader resolver = serviceProvider.GetService<ResolverTypesLoader>();

            }
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;
                ResolverTypesLoader resolver = serviceProvider.GetService<ResolverTypesLoader>();
            }
        }
    }
}
