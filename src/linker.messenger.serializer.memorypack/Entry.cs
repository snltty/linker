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
            MemoryPackFormatterProvider.Register(new SignInNamesResponseItemInfoFormatter());


            MemoryPackFormatterProvider.Register(new SyncInfoFormatter());


            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelRouteLevelInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelSetRouteLevelInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelInterfaceInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelNetInfoFormatter());


            MemoryPackFormatterProvider.Register(new DecenterSyncInfoFormatter());
            MemoryPackFormatterProvider.Register(new DecenterSyncInfo170Formatter());


            MemoryPackFormatterProvider.Register(new UpdaterConfirmInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterConfirmServerInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterClientInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfoFormatter());


            MemoryPackFormatterProvider.Register(new RelayTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayTestInfo170Formatter());
            MemoryPackFormatterProvider.Register(new RelayInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayInfo170Formatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfo170Formatter());
            MemoryPackFormatterProvider.Register(new RelayAskResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayAskResultInfo170Formatter());
            MemoryPackFormatterProvider.Register(new RelayCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayMessageInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyStoreInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyPageRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyPageResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyDelInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayTrafficUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyImportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyTestResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerCdkeyOrderInfoFormatter());

            MemoryPackFormatterProvider.Register(new RelayServerNodeUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeUpdateWrapInfoFormatter());


            MemoryPackFormatterProvider.Register(new AccessUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new AccessInfoFormatter());
            MemoryPackFormatterProvider.Register(new ApiPasswordUpdateInfoFormatter());


            MemoryPackFormatterProvider.Register(new Socks5LanInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5InfoFormatter());


            MemoryPackFormatterProvider.Register(new SForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardRemoveForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardProxyInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardCountInfoFormatter());


            MemoryPackFormatterProvider.Register(new ForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardAddForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardRemoveForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardCountInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardTestInfoFormatter());


            MemoryPackFormatterProvider.Register(new FlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new FlowReportNetInfoFormatter());

            MemoryPackFormatterProvider.Register(new FlowInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayFlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayFlowRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayFlowResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardFlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardFlowRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardFlowResponseInfoFormatter());

            MemoryPackFormatterProvider.Register(new TuntapVeaLanIPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TuntapVeaLanIPAddressListFormatter());
            MemoryPackFormatterProvider.Register(new TuntapInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardTestWrapInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapLanInfoFormatter());
            MemoryPackFormatterProvider.Register(new LeaseInfoFormatter());

            return serviceCollection;
        }
        public static ServiceProvider UseSerializerMemoryPack(this ServiceProvider serviceProvider)
        {
            return serviceProvider;
        }
    }
}
