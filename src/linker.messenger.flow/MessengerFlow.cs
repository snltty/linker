using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
        public override void AddStopwatch(ushort id, long time, MessageTypes type)
        {
            messengerFlow.AddStopwatch(id, time, type);
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

        private ConcurrentDictionary<ushort, FlowItemInfo> flows = new ConcurrentDictionary<ushort, FlowItemInfo>();
        private ConcurrentDictionary<ushort, FlowItemInfo> stopwatchs = new ConcurrentDictionary<ushort, FlowItemInfo>();

        public MessengerFlow()
        {
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<ushort, FlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; flows.Clear(); }

        public void AddReceive(ushort id, long bytes)
        {
            if (flows.TryGetValue(id, out FlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new FlowItemInfo();
                flows.TryAdd(id, messengerFlowItemInfo);
            }
            ReceiveBytes += bytes;
            messengerFlowItemInfo.ReceiveBytes += bytes;
            Version.Increment();
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
    }
}
