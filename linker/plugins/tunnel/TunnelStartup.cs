using linker.config;
using linker.startup;
using linker.tunnel;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using linker.tunnel.wanport;
using linker.plugins.client;
using linker.messenger.tunnel;

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
            //打洞协议
            serviceCollection.AddSingleton<TunnelTransfer>();
            serviceCollection.AddSingleton<TunnelExcludeIPTransfer>();
            serviceCollection.AddSingleton<TunnelMessengerAdapter>();

            //命令接口
            serviceCollection.AddSingleton<PlusTunnelClientMessenger>();
            serviceCollection.AddSingleton<ITunnelMessengerAdapterStore, PlusTunnelMessengerAdapterStore>();

            //序列化扩展
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());


            serviceCollection.AddSingleton<TunnelExcludeIPTypesLoader>();
            serviceCollection.AddSingleton<TunnelConfigTransfer>();
            serviceCollection.AddSingleton<TunnelConfigSyncTransports>();
            serviceCollection.AddSingleton<TunnelDecenter>();
            serviceCollection.AddSingleton<TunnelApiController>();

        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());

            serviceCollection.AddSingleton<PlusTunnelServerMessenger>();
            serviceCollection.AddSingleton<PlusTunnelExternalResolver, PlusTunnelExternalResolver>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            TunnelExcludeIPTransfer excludeIPTransfer = serviceProvider.GetService<TunnelExcludeIPTransfer>();
            TunnelExcludeIPTypesLoader tunnelExcludeIPTypesLoader = serviceProvider.GetService<TunnelExcludeIPTypesLoader>();

            ClientConfigTransfer clientConfigTransfer = serviceProvider.GetService<ClientConfigTransfer>();
            TunnelConfigTransfer tunnelConfigTransfer = serviceProvider.GetService<TunnelConfigTransfer>();

            ITunnelMessengerAdapterStore tunnelAdapter = serviceProvider.GetService<ITunnelMessengerAdapterStore>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {

        }
    }
}
