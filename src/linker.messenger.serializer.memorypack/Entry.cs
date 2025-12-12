using linker.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.serializer.memorypack
{
    public static class Entry
    {
        public static ServiceCollection AddSerializerMemoryPack(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISerializer, PlusMemoryPackSerializer>();

            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TunnelConnectionFormatter());
            MemoryPackFormatterProvider.Register(new ConnectionFormatter());

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
            MemoryPackFormatterProvider.Register(new SignInUserIdsResponseItemInfoFormatter());

            MemoryPackFormatterProvider.Register(new SignInPushArgInfoFormatter());


            MemoryPackFormatterProvider.Register(new SyncInfoFormatter());
            MemoryPackFormatterProvider.Register(new SyncInfoFormatter());


            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelWanPortProtocolInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelRouteLevelInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelNetworkInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelSetRouteLevelInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelInterfaceInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelNetInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelTransportItemSetInfoFormatter());


            MemoryPackFormatterProvider.Register(new DecenterSyncInfoFormatter());
            MemoryPackFormatterProvider.Register(new DecenterPullPageInfoFormatter());
            MemoryPackFormatterProvider.Register(new DecenterPullPageResultInfoFormatter());


            MemoryPackFormatterProvider.Register(new UpdaterConfirmInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterConfirmServerInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterClientInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterClientInfo170Formatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfoFormatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfo170Formatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfo170Formatter());
            MemoryPackFormatterProvider.Register(new UpdaterInfo170Formatter());
            MemoryPackFormatterProvider.Register(new UpdaterSyncInfoFormatter());


            MemoryPackFormatterProvider.Register(new NodeShareInfoFormatter());
            MemoryPackFormatterProvider.Register(new MastersRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterConnInfoFormatter());
            MemoryPackFormatterProvider.Register(new MastersResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterDenyStoreRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterDenyStoreResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterDenyStoreInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterDenyAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new MasterDenyDelInfoFormatter());


            MemoryPackFormatterProvider.Register(new RelayAskResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayMessageInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new RelayServerNodeStoreInfoFormatter());

            MemoryPackFormatterProvider.Register(new RelayServerNodeReportInfoFormatterOld());
            

            MemoryPackFormatterProvider.Register(new AccessUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new AccessBitsUpdateInfoFormatter());
            MemoryPackFormatterProvider.Register(new AccessInfoFormatter());
            MemoryPackFormatterProvider.Register(new AccessBotsInfoFormatter());
            MemoryPackFormatterProvider.Register(new ApiPasswordUpdateInfoFormatter());


            MemoryPackFormatterProvider.Register(new Socks5LanInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5InfoFormatter());


            MemoryPackFormatterProvider.Register(new SForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardAddForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardRemoveForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardProxyInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardServerNodeReportInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardServerNodeStoreInfoFormatter());
            MemoryPackFormatterProvider.Register(new SForwardServerNodeReportInfoOldFormatter());


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
            MemoryPackFormatterProvider.Register(new ForwardFlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardFlowRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new ForwardFlowResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5FlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5FlowRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new Socks5FlowResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelFlowItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelFlowRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new TunnelFlowResponseInfoFormatter());

            MemoryPackFormatterProvider.Register(new TuntapVeaLanIPAddressFormatter());
            MemoryPackFormatterProvider.Register(new TuntapVeaLanIPAddressListFormatter());
            MemoryPackFormatterProvider.Register(new TuntapInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardTestWrapInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapForwardTestInfoFormatter());
            MemoryPackFormatterProvider.Register(new TuntapLanInfoFormatter());
            MemoryPackFormatterProvider.Register(new LeaseInfoFormatter());


            MemoryPackFormatterProvider.Register(new PlanInfoFormatter());
            MemoryPackFormatterProvider.Register(new PlanGetInfoFormatter());
            MemoryPackFormatterProvider.Register(new PlanAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new PlanRemoveInfoFormatter());


            MemoryPackFormatterProvider.Register(new FirewallRuleInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallSearchInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallSearchForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallListInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallAddForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallRemoveForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallStateForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallCheckInfoFormatter());
            MemoryPackFormatterProvider.Register(new FirewallCheckForwardInfoFormatter());

            MemoryPackFormatterProvider.Register(new WakeupInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupSearchInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupSearchForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupAddForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupRemoveForwardInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupSendInfoFormatter());
            MemoryPackFormatterProvider.Register(new WakeupSendForwardInfoFormatter());


            MemoryPackFormatterProvider.Register(new WhiteListInfoFormatter());
            MemoryPackFormatterProvider.Register(new WhiteListAddInfoFormatter());
            MemoryPackFormatterProvider.Register(new WhiteListDelInfoFormatter());
            MemoryPackFormatterProvider.Register(new WhiteListPageRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new WhiteListPageResultInfoFormatter());
            MemoryPackFormatterProvider.Register(new WhiteListOrderStatusInfoFormatter());

            return serviceCollection;
        }
        public static ServiceProvider UseSerializerMemoryPack(this ServiceProvider serviceProvider)
        {
            return serviceProvider;
        }
    }
}
