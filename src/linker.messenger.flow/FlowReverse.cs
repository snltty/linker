using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.reverse.proxy;
using linker.messenger.reverse.server;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public sealed class FlowReverseProxy : ReverseProxy
    {
        private readonly FlowReverse reverseFlow;
        public FlowReverseProxy(FlowReverse reverseFlow, ReverseServerNodeTransfer reverseServerNodeTransfer) :base(reverseServerNodeTransfer)
        {
            this.reverseFlow = reverseFlow;
        }
        public override void Add(string key, string groupid, long recvBytes, long sentBytes)
        {
            reverseFlow.Add(key, groupid, recvBytes, sentBytes);
        }
    }

    public sealed class FlowReverse : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Reverse";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<string, ReverseFlowItemInfo> flows = new ConcurrentDictionary<string, ReverseFlowItemInfo>();

        public FlowReverse()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (lastTicksManager.DiffLessEqual(5000))
                {
                    foreach (var item in flows.Values)
                    {
                        item.DiffReceiveBytes = item.SendtBytes - item.OldSendtBytes;
                        item.DiffSendtBytes = item.ReceiveBytes - item.OldReceiveBytes;

                        item.OldSendtBytes = item.SendtBytes;
                        item.OldReceiveBytes = item.ReceiveBytes;
                    }
                }
            }, () => lastTicksManager.DiffLessEqual(5000) ? 1000 : 30000);
        }

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<string, ReverseFlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; flows.Clear(); }

        public void Update()
        {
            lastTicksManager.Update();
        }
        public (long, long) GetDiffBytes(long recv, long sent)
        {

            long diffRecv = ReceiveBytes - recv;
            long diffSendt = SendtBytes - sent;
            return (diffRecv, diffSendt);
        }

        public void Add(string key, string groupid, long recvBytes, long sentBytes)
        {
            if (flows.TryGetValue(key, out ReverseFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new ReverseFlowItemInfo { Key = key, GroupId = groupid };
                flows.TryAdd(key, messengerFlowItemInfo);
            }
            ReceiveBytes += recvBytes;
            messengerFlowItemInfo.ReceiveBytes += recvBytes;
            SendtBytes += sentBytes;
            messengerFlowItemInfo.SendtBytes += sentBytes;
            Version.Increment();
        }
        public ReverseFlowResponseInfo GetFlows(ReverseFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => string.IsNullOrWhiteSpace(info.Key) || c.Key.Contains(info.Key));
            if (string.IsNullOrWhiteSpace(info.GroupId) == false)
            {
                items = items.Where(c => c.GroupId == info.GroupId);
            }
            switch (info.Order)
            {
                case ReverseFlowOrder.Sendt:
                    if (info.OrderType == ReverseFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case ReverseFlowOrder.DiffSendt:
                    if (info.OrderType == ReverseFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffSendtBytes);
                    else
                        items = items.OrderBy(x => x.DiffSendtBytes);
                    break;
                case ReverseFlowOrder.Receive:
                    if (info.OrderType == ReverseFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                case ReverseFlowOrder.DiffRecive:
                    if (info.OrderType == ReverseFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffReceiveBytes);
                    else
                        items = items.OrderBy(x => x.DiffReceiveBytes);
                    break;
                default:
                    break;
            }
            ReverseFlowResponseInfo resp = new ReverseFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class ReverseFlowItemInfo : FlowItemInfo
    {
        public long DiffReceiveBytes { get; set; }
        public long DiffSendtBytes { get; set; }
        public string Key { get; set; }

        public string GroupId { get; set; }

        [JsonIgnore]
        public long OldReceiveBytes { get; set; }
        [JsonIgnore]
        public long OldSendtBytes { get; set; }
    }

    public sealed partial class ReverseFlowRequestInfo
    {
        public string Key { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public ReverseFlowOrder Order { get; set; }
        public ReverseFlowOrderType OrderType { get; set; }
    }

    public enum ReverseFlowOrder : byte
    {
        Sendt = 1,
        DiffSendt = 2,
        Receive = 3,
        DiffRecive = 4
    }
    public enum ReverseFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class ReverseFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<ReverseFlowItemInfo> Data { get; set; }
    }
}