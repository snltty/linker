using cmonitor.config;
using cmonitor.plugins.relay.messenger;
using cmonitor.plugins.relay.transport;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.relay
{
    public sealed class RelayStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "relay";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayApiController>();
            serviceCollection.AddSingleton<RelayClientMessenger>();
            serviceCollection.AddSingleton<TransportSelfHost>();
            serviceCollection.AddSingleton<RelayTransfer>();


            if (config.Data.Client.Relay.Servers.Length == 0)
            {
                config.Data.Client.Relay.Servers = new RelayCompactInfo[]
                {
                     new RelayCompactInfo{
                         Name="默认",
                         Type= RelayCompactType.Self,
                         Disabled = false,
                         Host = config.Data.Client.Servers.FirstOrDefault().Host
                     }
                };
            }
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RelayServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            RelayTransfer relayTransfer = serviceProvider.GetService<RelayTransfer>();
            relayTransfer.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
