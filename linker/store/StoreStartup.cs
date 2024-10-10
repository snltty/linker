using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.store
{
    /// <summary>
    /// 持久化加载插件
    /// </summary>
    public sealed class StoreStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "database";
        public bool Required => false;
        public string[] Dependent => Array.Empty<string>();
        public StartupLoadType LoadType => StartupLoadType.Normal;

        bool loaded = false;
        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            Add(serviceCollection, config);
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            Add(serviceCollection, config);
        }

        private void Add(ServiceCollection serviceCollection, FileConfig config)
        {
            if (loaded == false)
            {
                loaded = true;
                serviceCollection.AddSingleton<Storefactory>();
            }
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
