using Linker.Config;
using Linker.Plugins.Tunnel.Messenger;
using Linker.Startup;
using Linker.Tunnel;
using Linker.Tunnel.Adapter;
using Linker.Tunnel.Transport;
using Linker.Libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;
using Linker.Tunnel.WanPort;

namespace Linker.Plugins.Tunnel
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

        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            //序列化扩展
            MemoryPackFormatterProvider.Register(new TunnelWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());

            //管理接口
            serviceCollection.AddSingleton<TunnelApiController>();
            //命令接口
            serviceCollection.AddSingleton<TunnelClientMessenger>();

            //外网端口协议
            serviceCollection.AddSingleton<TunnelWanPortTransfer>();
            serviceCollection.AddSingleton<TunnelWanPortLinker>();
            serviceCollection.AddSingleton<TunnelWanPortStun>();

            //打洞协议
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelTransportTcpNutssb>();
            serviceCollection.AddSingleton<TransportMsQuic>();
            serviceCollection.AddSingleton<TransportMsQuic>();

            serviceCollection.AddSingleton<TunnelConfigTransfer>();
            serviceCollection.AddSingleton<ITunnelAdapter, TunnelAdapter>();

            LoggerHelper.Instance.Info($"tunnel route level getting.");
            config.Data.Client.Tunnel.RouteLevel = NetworkHelper.GetRouteLevel(out List<IPAddress> ips);
            LoggerHelper.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
            config.Data.Client.Tunnel.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
            LoggerHelper.Instance.Info($"tunnel local ips :{string.Join(",", config.Data.Client.Tunnel.LocalIPs.Select(c => c.ToString()))}");
            LoggerHelper.Instance.Info($"tunnel route level:{config.Data.Client.Tunnel.RouteLevel}");

        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            MemoryPackFormatterProvider.Register(new TunnelWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());

            serviceCollection.AddSingleton<TunnelServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
            ITunnelAdapter tunnelAdapter = serviceProvider.GetService<ITunnelAdapter>();

            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assemblies.Concat(new Assembly[] { typeof(TunnelWanPortTransfer).Assembly }).ToArray(), typeof(ITunnelWanPort));
            List<ITunnelWanPort> compacts = types.Select(c => (ITunnelWanPort)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            TunnelWanPortTransfer compack = serviceProvider.GetService<TunnelWanPortTransfer>();
            compack.Init(tunnelAdapter,compacts);


            types = ReflectionHelper.GetInterfaceSchieves(assemblies.Concat(new Assembly[] { typeof(TunnelTransfer).Assembly }).ToArray(), typeof(ITunnelTransport));
            List<ITunnelTransport> transports = types.Select(c => (ITunnelTransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            TunnelTransfer tunnel = serviceProvider.GetService<TunnelTransfer>();
            tunnel.Init(compack, tunnelAdapter, transports);

            TunnelConfigTransfer tunnelConfigTransfer = serviceProvider.GetService<TunnelConfigTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {

        }
    }
}
