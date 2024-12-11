using linker.config;
using linker.plugins.tunnel.messenger;
using linker.startup;
using linker.tunnel;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using linker.tunnel.wanport;
using linker.plugins.tunnel.excludeip;
using linker.plugins.client;

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

        public string[] Dependent => new string[] { "messenger", "signin", "serialize", "config" };

        public StartupLoadType LoadType => StartupLoadType.Normal;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            //序列化扩展
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());


            //管理接口
            serviceCollection.AddSingleton<TunnelApiController>();
            //命令接口
            serviceCollection.AddSingleton<TunnelClientMessenger>();

            //外网端口协议
            serviceCollection.AddSingleton<TunnelWanPortTransfer>();
            serviceCollection.AddSingleton<TunnelWanPortProtocolLinkerUdp>();
            serviceCollection.AddSingleton<TunnelWanPortProtocolLinkerTcp>();


            //打洞协议
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelTransportTcpNutssb>();
            serviceCollection.AddSingleton<TransportMsQuic>();
            serviceCollection.AddSingleton<TransportTcpP2PNAT>();
            serviceCollection.AddSingleton<TransportTcpPortMap>();
            serviceCollection.AddSingleton<TransportUdpPortMap>();
            serviceCollection.AddSingleton<TransportUdp>();


            serviceCollection.AddSingleton<TunnelExcludeIPTransfer>();
            serviceCollection.AddSingleton<TunnelExcludeIPTypesLoader>();
            serviceCollection.AddSingleton<TunnelConfigTransfer>();
            serviceCollection.AddSingleton<ITunnelAdapter, TunnelAdapter>();

            serviceCollection.AddSingleton<TunnelUpnpTransfer>();

            serviceCollection.AddSingleton<TunnelConfigSyncTransports>();

            serviceCollection.AddSingleton<TunnelConfigTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());

            serviceCollection.AddSingleton<TunnelServerMessenger>();
            serviceCollection.AddSingleton<ExternalResolver, ExternalResolver>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            ITunnelAdapter tunnelAdapter = serviceProvider.GetService<ITunnelAdapter>();
            TunnelUpnpTransfer upnpTransfer = serviceProvider.GetService<TunnelUpnpTransfer>();

            IEnumerable<Type> types = new List<Type> {
                typeof(TunnelWanPortProtocolLinkerUdp),
                typeof(TunnelWanPortProtocolLinkerTcp)
            };
            List<ITunnelWanPortProtocol> compacts = types.Select(c => (ITunnelWanPortProtocol)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            TunnelWanPortTransfer compack = serviceProvider.GetService<TunnelWanPortTransfer>();
            compack.LoadTransports(compacts);


            types = new List<Type> {
                typeof(TunnelTransportTcpNutssb),
                typeof(TransportMsQuic),
                typeof(TransportTcpP2PNAT),
                typeof(TransportTcpPortMap),
                typeof(TransportUdpPortMap),
                typeof(TransportUdp),
            };
            List<ITunnelTransport> transports = types.Select(c => (ITunnelTransport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            TunnelTransfer tunnel = serviceProvider.GetService<TunnelTransfer>();
            tunnel.LoadTransports(compack, tunnelAdapter, upnpTransfer, transports);

            TunnelExcludeIPTransfer excludeIPTransfer = serviceProvider.GetService<TunnelExcludeIPTransfer>();
            TunnelExcludeIPTypesLoader tunnelExcludeIPTypesLoader = serviceProvider.GetService<TunnelExcludeIPTypesLoader>();

            ClientConfigTransfer clientConfigTransfer = serviceProvider.GetService<ClientConfigTransfer>();
            TunnelConfigTransfer tunnelConfigTransfer = serviceProvider.GetService<TunnelConfigTransfer>();

            LoggerHelper.Instance.Info($"tunnel route level getting.");
            tunnelConfigTransfer.RefreshRouteLevel();
            LoggerHelper.Instance.Warning($"route ips:{string.Join(",", tunnelConfigTransfer.RouteIPs.Select(c => c.ToString()))}");
            LoggerHelper.Instance.Warning($"tunnel local ips :{string.Join(",", tunnelConfigTransfer.LocalIPs.Select(c => c.ToString()))}");
            LoggerHelper.Instance.Warning($"tunnel route level:{tunnelConfigTransfer.RouteLevel}");
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {

        }
    }
}
