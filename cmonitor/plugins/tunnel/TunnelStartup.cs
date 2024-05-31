using cmonitor.config;
using cmonitor.plugins.tunnel.compact;
using cmonitor.plugins.tunnel.messenger;
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
        public string Name => "tunnel";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TunnelApiController>();

            serviceCollection.AddSingleton<TunnelClientMessenger>();

            serviceCollection.AddSingleton<TunnelCompactTransfer>();
            serviceCollection.AddSingleton<TunnelCompactSelfHost>();
            serviceCollection.AddSingleton<TunnelCompactStun>();

            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelTransportTcpNutssb>();
            serviceCollection.AddSingleton<TransportMsQuic>();


            Logger.Instance.Info($"tunnel route level getting.");
            config.Data.Client.Tunnel.RouteLevel = NetworkHelper.GetRouteLevel(out List<IPAddress> ips);
            Logger.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
            config.Data.Client.Tunnel.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
            Logger.Instance.Info($"tunnel local ips :{string.Join(",", config.Data.Client.Tunnel.LocalIPs.Select(c => c.ToString()))}");
            Logger.Instance.Info($"tunnel route level:{config.Data.Client.Tunnel.RouteLevel}");

            if (config.Data.Client.Tunnel.Servers.Length == 0)
            {
                config.Data.Client.Tunnel.Servers = new TunnelCompactInfo[]
                {
                     new TunnelCompactInfo{
                         Name="默认",
                         Type= TunnelCompactType.Self,
                         Disabled = false,
                         Host = config.Data.Client.Servers.FirstOrDefault().Host
                     }
                };
            }
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<TunnelServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            TunnelCompactTransfer compack = serviceProvider.GetService<TunnelCompactTransfer>();
            compack.Load(assemblies);

            TunnelTransfer tunnel = serviceProvider.GetService<TunnelTransfer>();
            tunnel.Load(assemblies);
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }
    }
}
