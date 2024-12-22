using linker.config;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
using linker.messenger.relay.server.caching;
using linker.messenger.relay.server.validator;
using linker.plugins.relay.client;
using linker.plugins.relay.messenger;
using linker.plugins.relay.server;
using linker.plugins.relay.server.validator;
using linker.startup;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;

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

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<RelayClientTransfer>();

           
            serviceCollection.AddSingleton<PlusRelayClientMessenger>();
            serviceCollection.AddSingleton<IRelayClientStore, PlusRelayClientStore>();


            MemoryPackFormatterProvider.Register(new RelayTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayAskResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayMessageInfoFormatter());


            serviceCollection.AddSingleton<RelayApiController>();
            serviceCollection.AddSingleton<RelayClientTestTransfer>();
            serviceCollection.AddSingleton<RelayClientTypesLoader>();
            serviceCollection.AddSingleton<RelaConfigSyncSecretKey>();
            serviceCollection.AddSingleton<RelayClientConfigTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {

            serviceCollection.AddSingleton<RelayServerMasterTransfer>();
            serviceCollection.AddSingleton<RelayServerNodeTransfer>();
            serviceCollection.AddSingleton<IRelayServerCaching, RelayServerCachingMemory>();
            serviceCollection.AddSingleton<RelayServerValidatorTransfer>();
            serviceCollection.AddSingleton<IRelayServerNodeStore, PlusRelayServerNodeStore>();
            serviceCollection.AddSingleton<IRelayServerMasterStore, PlusRelayServerMasterStore>();

            serviceCollection.AddSingleton<PlusRelayServerMessenger>();
            serviceCollection.AddSingleton<PlusRelayServerResolver>();
            serviceCollection.AddSingleton<PlusRelayServerReportResolver>();

            MemoryPackFormatterProvider.Register(new RelayTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayAskResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayMessageInfoFormatter());


            serviceCollection.AddSingleton<RelayServerValidatorTypeLoader>();
            serviceCollection.AddSingleton<RelayServerValidatorSecretKey>();
            serviceCollection.AddSingleton<RelayServerConfigTransfer>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            RelayClientTransfer relayTransfer = serviceProvider.GetService<RelayClientTransfer>();
            RelayClientTypesLoader relayTypesLoader = serviceProvider.GetService<RelayClientTypesLoader>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            RelayServerValidatorTypeLoader relayValidatorTypeLoader = serviceProvider.GetService<RelayServerValidatorTypeLoader>();
            IRelayServerCaching relayCaching = serviceProvider.GetService<IRelayServerCaching>();

            PlusRelayServerReportResolver relayReportResolver = serviceProvider.GetService<PlusRelayServerReportResolver>();


            RelayServerMasterTransfer relayServerMasterTransfer = serviceProvider.GetService<RelayServerMasterTransfer>();
            RelayServerNodeTransfer relayServerNodeTransfer = serviceProvider.GetService<RelayServerNodeTransfer>();
        }
    }
}
