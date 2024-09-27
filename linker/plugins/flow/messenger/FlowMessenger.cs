using linker.plugins.messenger;
using linker.plugins.sforward.proxy;
using MemoryPack;

namespace linker.plugins.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly MessengerResolver messengerResolver;
        private readonly FlowTransfer flowTransfer;
        private readonly MessengerFlow messengerFlow;
        private readonly SForwardFlow sForwardFlow;

        private DateTime start = DateTime.Now;

        public FlowMessenger(MessengerResolver messengerResolver, FlowTransfer flowTransfer, MessengerFlow messengerFlow, SForwardFlow sForwardFlow)
        {
            this.messengerResolver = messengerResolver;
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = flowTransfer.GetFlows(),
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
    }

}
