using linker.plugins.messenger;

namespace linker.plugins.flow
{

    public sealed class MessengerResolverFlow : MessengerResolver
    {
        private readonly MessengerFlow messengerFlow;
        public MessengerResolverFlow(IMessengerSender sender, MessengerFlow messengerFlow) : base(sender)
        {
            this.messengerFlow = messengerFlow;
        }

        public override void AddReceive(ushort id, ulong bytes)
        {
            messengerFlow.AddReceive(id, bytes);
        }
        public override void AddSendt(ushort id, ulong bytes)
        {
            messengerFlow.AddSendt(id, bytes);
        }
    }
    public sealed class MessengerSenderFlow : MessengerSender
    {
        private readonly MessengerFlow messengerFlow;
        public MessengerSenderFlow(MessengerFlow messengerFlow)
        {
            this.messengerFlow = messengerFlow;
        }

        public override void AddReceive(ushort id, ulong bytes)
        {
            messengerFlow.AddReceive(id, bytes);
        }
        public override void AddSendt(ushort id, ulong bytes)
        {
            messengerFlow.AddSendt(id, bytes);
        }
    }



    public sealed class MessengerFlow : IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "Messenger";

        private Dictionary<ushort, FlowItemInfo> flows { get; } = new Dictionary<ushort, FlowItemInfo>();

        public MessengerFlow()
        {
        }

        public void AddReceive(ushort id, ulong bytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            ReceiveBytes += bytes;
            messengerFlowItemInfo.ReceiveBytes += bytes;
        }
        public void AddSendt(ushort id, ulong bytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            SendtBytes += bytes;
            messengerFlowItemInfo.SendtBytes += bytes;
        }

        public Dictionary<ushort, FlowItemInfo> GetFlows()
        {
            return flows;
        }
    }
}
