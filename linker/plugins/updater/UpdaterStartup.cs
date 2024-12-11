using linker.config;
using linker.plugins.updater.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

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

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterClientTransfer>();

            serviceCollection.AddSingleton<UpdaterClientMessenger>();
            serviceCollection.AddSingleton<UpdaterClientApiController>();

            serviceCollection.AddSingleton<UpdaterConfigSyncSecretKey>();

            serviceCollection.AddSingleton<UpdaterCommonTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<UpdaterHelper>();
            serviceCollection.AddSingleton<UpdaterServerTransfer>();

            serviceCollection.AddSingleton<UpdaterServerMessenger>();

            serviceCollection.AddSingleton<UpdaterCommonTransfer>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            _ = serviceProvider.GetService<UpdaterClientTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            _ = serviceProvider.GetService<UpdaterServerTransfer>();
        }
    }
}
