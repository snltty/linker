using linker.libs;
using linker.libs.extends;

namespace linker.messenger.flow
{

    public sealed class MessengerResolverFlow : MessengerResolver
    {
        private readonly MessengerFlow messengerFlow;
        public MessengerResolverFlow(IMessengerSender sender, MessengerFlow messengerFlow, IMessengerStore messengerStore) : base(sender, messengerStore)
        {
            this.messengerFlow = messengerFlow;
        }

        public override void AddReceive(ushort id, long bytes)
        {
            messengerFlow.AddReceive(id, bytes);
        }
        public override void AddSendt(ushort id, long bytes)
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

        public override void AddReceive(ushort id, long bytes)
        {
            messengerFlow.AddReceive(id, bytes);
        }
        public override void AddSendt(ushort id, long bytes)
        {
            messengerFlow.AddSendt(id, bytes);
        }
    }



    public sealed class MessengerFlow : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Messenger";
        public VersionManager Version { get; } = new VersionManager();

        private Dictionary<ushort, FlowItemInfo> flows = new Dictionary<ushort, FlowItemInfo>();

        public MessengerFlow()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<Dictionary<ushort, FlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0;flows.Clear(); }

        public void AddReceive(ushort id, long bytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            ReceiveBytes += bytes;
            messengerFlowItemInfo.ReceiveBytes += bytes;
            Version.Add();
        }
        public void AddSendt(ushort id, long bytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            SendtBytes += bytes;
            messengerFlowItemInfo.SendtBytes += bytes;
            Version.Add();
        }

        public Dictionary<ushort, FlowItemInfo> GetFlows()
        {
            return flows;
        }
    }
}
