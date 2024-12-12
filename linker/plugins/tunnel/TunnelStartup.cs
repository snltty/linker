using linker.config;
using linker.plugins.tunnel.messenger;
using linker.startup;
using linker.tunnel;
using linker.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
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
            //打洞协议
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelUpnpTransfer>();


            serviceCollection.AddSingleton<TunnelExcludeIPTransfer>();
            serviceCollection.AddSingleton<TunnelExcludeIPTypesLoader>();
            serviceCollection.AddSingleton<TunnelConfigTransfer>();
            serviceCollection.AddSingleton<TunnelConfigSyncTransports>();
            serviceCollection.AddSingleton<TunnelDecenter>();

            serviceCollection.AddSingleton<TunnelAdapter>();
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
            TunnelExcludeIPTransfer excludeIPTransfer = serviceProvider.GetService<TunnelExcludeIPTransfer>();
            TunnelExcludeIPTypesLoader tunnelExcludeIPTypesLoader = serviceProvider.GetService<TunnelExcludeIPTypesLoader>();

            ClientConfigTransfer clientConfigTransfer = serviceProvider.GetService<ClientConfigTransfer>();
            TunnelConfigTransfer tunnelConfigTransfer = serviceProvider.GetService<TunnelConfigTransfer>();

            TunnelAdapter tunnelAdapter = serviceProvider.GetService<TunnelAdapter>();

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
