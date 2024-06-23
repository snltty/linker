using linker.config;
using linker.plugins.tunnel.messenger;
using linker.startup;
using linker.tunnel;
using linker.tunnel.adapter;
using linker.tunnel.compact;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 打洞插件
    /// </summary>
    public sealed class TunnelStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "tunnel";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            //序列化扩展
            MemoryPackFormatterProvider.Register(new TunnelCompactInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportExternalIPInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());

            //管理接口
            serviceCollection.AddSingleton<TunnelApiController>();
            //命令接口
            serviceCollection.AddSingleton<TunnelClientMessenger>();

            //外网端口协议
            serviceCollection.AddSingleton<TunnelCompactTransfer>();
            serviceCollection.AddSingleton<TunnelCompactSelfHost>();
            serviceCollection.AddSingleton<TunnelCompactStun>();

            //打洞协议
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelTransportTcpNutssb>();
            serviceCollection.AddSingleton<TransportMsQuic>();
            serviceCollection.AddSingleton<TransportMsQuic>();
            //serviceCollection.AddSingleton<TransportMsQuicTest>();
            //serviceCollection.AddSingleton<TransportUdp>();

            serviceCollection.AddSingleton<TunnelConfigTransfer>();
            serviceCollection.AddSingleton<ITunnelAdapter, TunnelAdapter>();

            Logger.Instance.Info($"tunnel route level getting.");
            config.Data.Client.Tunnel.RouteLevel = NetworkHelper.GetRouteLevel(out List<IPAddress> ips);
            Logger.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
            config.Data.Client.Tunnel.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
            Logger.Instance.Info($"tunnel local ips :{string.Join(",", config.Data.Client.Tunnel.LocalIPs.Select(c => c.ToString()))}");
            Logger.Instance.Info($"tunnel route level:{config.Data.Client.Tunnel.RouteLevel}");

        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            MemoryPackFormatterProvider.Register(new TunnelCompactInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportExternalIPInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());

            serviceCollection.AddSingleton<TunnelServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            TunnelCompactTransfer compack = serviceProvider.GetService<TunnelCompactTransfer>();
            compack.Load(assemblies);

            TunnelTransfer tunnel = serviceProvider.GetService<TunnelTransfer>();
            tunnel.Load(assemblies);

            TunnelConfigTransfer tunnelConfigTransfer = serviceProvider.GetService<TunnelConfigTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {

        }
    }
}
