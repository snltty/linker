using linker.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.serializer.memorypack
{
    public static class Entry
    {
        public static ServiceCollection AddSerializerMemoryPack(this ServiceCollection serviceCollection)
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TunnelConnectionFormatter());
            MemoryPackFormatterProvider.Register(new ConnectionFormatter());


            serviceCollection.AddSingleton<ISerializer, PlusMemoryPackSerializer>();

            MemoryPackFormatterProvider.Register(new SignInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInConfigSetNameInfoFormatter());


            MemoryPackFormatterProvider.Register(new SyncInfoFormatter());


            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelRouteLevelInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelSetRouteLevelInfoFormatter());


            MemoryPackFormatterProvider.Register(new DecenterSyncInfoFormatter());


            MemoryPackFormatterProvider.Register(new UpdaterConfirmInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterConfirmServerInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterClientInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfoFormatter());


            MemoryPackFormatterProvider.Register(new RelayTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayAskResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayMessageInfoFormatter());

            MemoryPackFormatterProvider.Register(new AccessUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new AccessInfoFormatter());

            MemoryPackFormatterProvider.Register(new Socks5LanInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5InfoFormatter());

            return serviceCollection;
        }
        public static ServiceProvider UseSerializerMemoryPack(this ServiceProvider serviceProvider)
        {
            return serviceProvider;
        }
    }
}
