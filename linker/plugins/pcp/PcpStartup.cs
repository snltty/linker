using linker.config;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.pcp
{
    /// <summary>
    /// 节点中继插件
    /// </summary>
    public sealed class PcpStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "pcp";

        public bool Required => false;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<PcpConfigTransfer>();
            serviceCollection.AddSingleton<PcpTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            PcpTransfer pcpTransfer = serviceProvider.GetService<PcpTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
        }
    }
}
