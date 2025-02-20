using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.sforward.server;
using linker.messenger.signin;

namespace linker.messenger.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly FlowTransfer flowTransfer;
        private readonly MessengerFlow messengerFlow;
        private readonly SForwardFlow sForwardFlow;
        private readonly RelayFlow relayFlow;
        private readonly SignInServerCaching signCaching;
        private readonly IRelayServerStore relayServerStore;
        private readonly ISForwardServerStore sForwardServerStore;
        private readonly ISerializer serializer;
        private readonly FlowResolver flowResolver;

        private DateTime start = DateTime.Now;

        public FlowMessenger(FlowTransfer flowTransfer, MessengerFlow messengerFlow, SForwardFlow sForwardFlow, RelayFlow relayFlow, SignInServerCaching signCaching, IRelayServerStore relayServerStore, ISForwardServerStore sForwardServerStore, ISerializer serializer, FlowResolver flowResolver)
        {
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
            this.relayFlow = relayFlow;
            this.signCaching = signCaching;
            this.relayServerStore = relayServerStore;
            this.sForwardServerStore = sForwardServerStore;
            this.serializer = serializer;
            this.flowResolver = flowResolver;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();

            signCaching.GetOnline(out int all, out int online);
            dic.TryAdd("_", new FlowItemInfo { FlowName = "_", ReceiveBytes = (ulong)all, SendtBytes = (ulong)online });

            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = dic,
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(serializer.Serialize(serverFlowInfo));
        }
        [MessengerId((ushort)FlowMessengerIds.Citys)]
        public void Citys(IConnection connection)
        {
            connection.Write(serializer.Serialize(flowResolver.GetCitys()));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetFlows()));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = serializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (sForwardServerStore.SecretKey == info.SecretKey)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    info.GroupId = cache.GroupId;
                }
                else
                {
                    info.GroupId = Guid.NewGuid().ToString();
                }
            }

            connection.Write(serializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            relayFlow.Update();
            RelayFlowRequestInfo info = serializer.Deserialize<RelayFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (relayServerStore.SecretKey == info.SecretKey)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    info.GroupId = cache.GroupId;
                }
                else
                {
                    info.GroupId = Guid.NewGuid().ToString();
                }
            }

            connection.Write(serializer.Serialize(relayFlow.GetFlows(info)));
        }
    }

}
