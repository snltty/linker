using Linker.Config;
using Linker.Plugins.Relay.Messenger;
using Linker.Plugins.Relay.Transport;
using Linker.Startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Linker.Plugins.Relay
{
    /// <summary>
    /// 中继插件
    /// </summary>
    public sealed class RelayStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "relay";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayApiController>();
            serviceCollection.AddSingleton<RelayClientMessenger>();
            serviceCollection.AddSingleton<TransportSelfHost>();
            serviceCollection.AddSingleton<RelayTransfer>();

        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            RelayTransfer relayTransfer = serviceProvider.GetService<RelayTransfer>();
            relayTransfer.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }
    }
}
