using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {

            serviceCollection.AddSingleton<ResolverTransfer>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<ResolverTransfer>();

        }


        private bool loaded = false;
        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                loaded = true;

                ResolverTransfer resolver = serviceProvider.GetService<ResolverTransfer>();
                resolver.LoadResolvers(assemblies);

            }
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            if (loaded == false)
            {
                loaded = true;

                ResolverTransfer resolver = serviceProvider.GetService<ResolverTransfer>();
                resolver.LoadResolvers(assemblies);
            }
        }
    }
}
