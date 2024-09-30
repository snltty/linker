using linker.plugins.messenger;
using linker.plugins.relay;
using linker.plugins.sforward.proxy;
using linker.plugins.signin.messenger;
using MemoryPack;

namespace linker.plugins.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly MessengerResolver messengerResolver;
        private readonly FlowTransfer flowTransfer;
        private readonly MessengerFlow messengerFlow;
        private readonly SForwardFlow sForwardFlow;
        private readonly RelayFlow relayFlow;
        private readonly SignCaching signCaching;

        private DateTime start = DateTime.Now;

        public FlowMessenger(MessengerResolver messengerResolver, FlowTransfer flowTransfer, MessengerFlow messengerFlow, SForwardFlow sForwardFlow, RelayFlow relayFlow, SignCaching signCaching)
        {
            this.messengerResolver = messengerResolver;
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
            this.relayFlow = relayFlow;
            this.signCaching = signCaching;
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
            connection.Write(MemoryPackSerializer.Serialize(serverFlowInfo));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(messengerFlow.GetFlows()));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = MemoryPackSerializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(MemoryPackSerializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            relayFlow.Update();
            RelayFlowRequestInfo info = MemoryPackSerializer.Deserialize<RelayFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(MemoryPackSerializer.Serialize(relayFlow.GetFlows(info)));
        }
    }

}
