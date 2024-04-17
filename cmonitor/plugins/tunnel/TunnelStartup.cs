using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.server;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<CompactTransfer>();
            serviceCollection.AddSingleton<CompactSelfHost>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TunnelServer>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            CompactTransfer transfer = serviceProvider.GetService<CompactTransfer>();
            transfer.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"use tunnel server in server mode.");
            TunnelServer tunnelServer = serviceProvider.GetService<TunnelServer>();
            tunnelServer.Start(config.Data.Server.Tunnel.ListenPort);
            Logger.Instance.Info($"start tunnel server, port : {config.Data.Server.Tunnel.ListenPort}");
        }
    }
}
