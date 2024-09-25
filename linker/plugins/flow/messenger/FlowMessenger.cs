using linker.plugins.messenger;
using MemoryPack;

namespace linker.plugins.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly MessengerResolver messengerResolver;
        private readonly FlowTransfer flowTransfer;
        private readonly MessengerFlow messengerFlow;

        private DateTime start = DateTime.Now;

        public FlowMessenger(MessengerResolver messengerResolver, FlowTransfer flowTransfer, MessengerFlow messengerFlow)
        {
            this.messengerResolver = messengerResolver;
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            FlowInfo serverFlowInfo = new FlowInfo
            {
                Messangers = messengerFlow.GetFlows(),
                Resolvers = flowTransfer.GetFlows(),
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(MemoryPackSerializer.Serialize(serverFlowInfo));
        }

    }

}
