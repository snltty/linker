using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;

namespace linker.messenger.flow
{

    public sealed class MessengerResolverFlow : MessengerResolver
    {
        private readonly FlowMessenger messengerFlow;
        public MessengerResolverFlow(IMessengerSender sender, FlowMessenger messengerFlow, IMessengerStore messengerStore) : base(sender, messengerStore)
        {
            this.messengerFlow = messengerFlow;
        }

        public override void Add(ushort id, long receiveBytes, long sendtBytes)
        {
            messengerFlow.Add(id, receiveBytes, sendtBytes);
        }

        public override void AddStopwatch(ushort id, long time, MessageTypes type)
        {
            messengerFlow.AddStopwatch(id, time, type);
        }
    }
    public sealed class MessengerSenderFlow : MessengerSender
    {
        private readonly FlowMessenger messengerFlow;
        public MessengerSenderFlow(FlowMessenger messengerFlow)
        {
            this.messengerFlow = messengerFlow;
        }

        public override void Add(ushort id, long receiveBytes, long sendtBytes)
        {
            messengerFlow.Add(id, receiveBytes, sendtBytes);
        }
    }

    public sealed class FlowMessenger : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Messenger";
        public VersionManager Version { get; } = new VersionManager();

        private ConcurrentDictionary<ushort, FlowItemInfo> flows = new ConcurrentDictionary<ushort, FlowItemInfo>();
        private readonly ConcurrentDictionary<ushort, FlowItemInfo> stopwatchs = new ConcurrentDictionary<ushort, FlowItemInfo>();

        public FlowMessenger()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<ushort, FlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; flows.Clear(); }

        public void Add(ushort id, long receiveBytes, long sendtBytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            ReceiveBytes += receiveBytes;
            messengerFlowItemInfo.ReceiveBytes += receiveBytes;
            SendtBytes += sendtBytes;
            messengerFlowItemInfo.SendtBytes += sendtBytes;
            Version.Increment();
        }
        public Dictionary<ushort, FlowItemInfo> GetFlows()
        {
            return flows.ToDictionary();
        }

        public void AddStopwatch(ushort id, long time, MessageTypes type)
        {
            long mask = -1L << 32;
            if (stopwatchs.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                stopwatchs.TryAdd(id, messengerFlowItemInfo);
            }
            switch (type)
            {
                case MessageTypes.REQUEST:
                    if (time >= (messengerFlowItemInfo.SendtBytes >> 32))
                        messengerFlowItemInfo.SendtBytes = (messengerFlowItemInfo.SendtBytes & 0xffffffff) | (time << 32);
                    messengerFlowItemInfo.SendtBytes = (messengerFlowItemInfo.SendtBytes & mask) | (uint)time;
                    break;
                case MessageTypes.RESPONSE:
                    if (time >= (messengerFlowItemInfo.ReceiveBytes >> 32))
                        messengerFlowItemInfo.ReceiveBytes = (messengerFlowItemInfo.ReceiveBytes & 0xffffffff) | (time << 32);
                    messengerFlowItemInfo.ReceiveBytes = (messengerFlowItemInfo.ReceiveBytes & mask) | (uint)time;
                    break;
                default:
                    break;
            }
        }
        public Dictionary<ushort, FlowItemInfo> GetStopwatch()
        {
            return stopwatchs.ToDictionary();
        }

        public (long, long) GetDiffBytes(long recv, long sent)
        {

            long diffRecv = ReceiveBytes - recv;
            long diffSendt = SendtBytes - sent;
            return (diffRecv, diffSendt);
        }
    }

}
