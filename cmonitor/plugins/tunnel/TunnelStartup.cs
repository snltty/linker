using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.messenger;
using cmonitor.plugins.tunnel.server;
using cmonitor.plugins.tunnel.transport;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TunnelApiController>();

            serviceCollection.AddSingleton<TunnelClientMessenger>();

            serviceCollection.AddSingleton<CompactTransfer>();
            serviceCollection.AddSingleton<CompactSelfHost>();

            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelBindServer>();
            serviceCollection.AddSingleton<TransportTcpNutssb>();


            Logger.Instance.Info($"tunnel route level getting.");
            config.Data.Client.Tunnel.RouteLevel = NetworkHelper.GetRouteLevel(out List<IPAddress> ips);
            config.Data.Client.Tunnel.LocalIPs = ips.Concat(NetworkHelper.GetIPV6()).ToArray();
            Logger.Instance.Info($"tunnel local ips :{string.Join(",", config.Data.Client.Tunnel.LocalIPs.Select(c=>c.ToString()))}");
            Logger.Instance.Info($"tunnel route level:{config.Data.Client.Tunnel.RouteLevel}");

            if (config.Data.Client.Tunnel.Servers.Length == 0)
            {
                config.Data.Client.Tunnel.Servers = new TunnelCompactInfo[]
                {
                     new TunnelCompactInfo{ Name="self", Disabled = false, Host = config.Data.Client.Server }
                };
            }
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TunnelServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            CompactTransfer compack = serviceProvider.GetService<CompactTransfer>();
            compack.Load(assemblies);

            TunnelTransfer tunnel = serviceProvider.GetService<TunnelTransfer>();
            tunnel.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }
    }
}
