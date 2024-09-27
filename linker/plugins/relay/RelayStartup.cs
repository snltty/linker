using linker.config;
using linker.plugins.relay.messenger;
using linker.plugins.relay.transport;
using linker.plugins.relay.validator;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继插件
    /// </summary>
    public sealed class RelayStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "relay";

        public bool Required => false;

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayApiController>();
            serviceCollection.AddSingleton<RelayClientMessenger>();
            serviceCollection.AddSingleton<TransportSelfHost>();
            serviceCollection.AddSingleton<RelayTransfer>();
            serviceCollection.AddSingleton<RelayFlow>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayServerMessenger>();

            serviceCollection.AddSingleton<RelayResolver>();

            serviceCollection.AddSingleton<RelayValidatorTransfer>();
            serviceCollection.AddSingleton<RelayValidatorSecretKey>();

            serviceCollection.AddSingleton<RelayFlow>();

        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            RelayTransfer relayTransfer = serviceProvider.GetService<RelayTransfer>();
            relayTransfer.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config, Assembly[] assemblies)
        {
            RelayValidatorTransfer relayValidatorTransfer = serviceProvider.GetService<RelayValidatorTransfer>();
        }
    }
}
