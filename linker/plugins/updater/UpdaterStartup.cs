using linker.config;
using linker.plugins.updater.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.updater
{
    /// <summary>
    /// 自动更新组件
    /// </summary>
    public sealed class UpdaterStartup : IStartup
    {
        public string Name => "updater";

        public bool Required => false;

        public StartupLevel Level => StartupLevel.Normal;

        public string[] Dependent => Array.Empty<string>();

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterClientTransfer>();

            serviceCollection.AddSingleton<UpdaterClientMessenger>();
            serviceCollection.AddSingleton<UpdaterClientApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterServerTransfer>();

            serviceCollection.AddSingleton<UpdaterServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            _ = serviceProvider.GetService<UpdaterClientTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            _ = serviceProvider.GetService<UpdaterServerTransfer>();
        }
    }
}
