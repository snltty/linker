using linker.config;
using linker.plugins.tuntap.client;
using linker.plugins.tuntap.lease;
using linker.plugins.tuntap.messenger;
using linker.startup;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.tuntap
{
    /// <summary>
    /// 虚拟网卡组网插件
    /// </summary>
    public sealed class TuntapStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "tuntap";
        public bool Required => false;
        public string[] Dependent => new string[] { "messenger", "relay", "tunnel", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<TuntapClientApiController>();
            serviceCollection.AddSingleton<LinkerTunDeviceAdapter>();
            serviceCollection.AddSingleton<TuntapTransfer>();
            serviceCollection.AddSingleton<TuntapProxy>();

            serviceCollection.AddSingleton<TuntapClientMessenger>();
            serviceCollection.AddSingleton<LeaseClientTreansfer>();

            serviceCollection.AddSingleton<ExcludeIP>();

            serviceCollection.AddSingleton<TuntapConfigTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<TuntapServerMessenger>();
            serviceCollection.AddSingleton<LeaseServerTreansfer>();

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            TuntapProxy tuntapProxy = serviceProvider.GetService<TuntapProxy>();
            TuntapTransfer tuntapTransfer = serviceProvider.GetService<TuntapTransfer>();

            LeaseClientTreansfer leaseTreansfer = serviceProvider.GetService<LeaseClientTreansfer>();

            TuntapConfigTransfer tuntapConfigTransfer = serviceProvider.GetService<TuntapConfigTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            LeaseServerTreansfer leaseTreansfer = serviceProvider.GetService<LeaseServerTreansfer>();
        }
    }
}
