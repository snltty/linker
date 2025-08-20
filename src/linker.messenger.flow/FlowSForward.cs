using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.sforward.server;
using linker.plugins.sforward.proxy;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public sealed class FlowSForwardProxy : SForwardProxy
    {
        private readonly FlowSForward sForwardFlow;
        public FlowSForwardProxy(FlowSForward sForwardFlow, SForwardServerNodeTransfer sForwardServerNodeTransfer) :base(sForwardServerNodeTransfer)
        {
            this.sForwardFlow = sForwardFlow;
        }
        public override void Add(string key, string groupid, long recvBytes, long sentBytes)
        {
            sForwardFlow.Add(key, groupid, recvBytes, sentBytes);
        }
    }

    public sealed class FlowSForward : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "SForward";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<string, SForwardFlowItemInfo> flows = new ConcurrentDictionary<string, SForwardFlowItemInfo>();

        public FlowSForward()
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
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<string, SForwardFlowItemInfo>>(); }
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
            if (flows.TryGetValue(key, out SForwardFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new SForwardFlowItemInfo { Key = key, GroupId = groupid };
                flows.TryAdd(key, messengerFlowItemInfo);
            }
            ReceiveBytes += recvBytes;
            messengerFlowItemInfo.ReceiveBytes += recvBytes;
            SendtBytes += sentBytes;
            messengerFlowItemInfo.SendtBytes += sentBytes;
            Version.Increment();
        }
        public SForwardFlowResponseInfo GetFlows(SForwardFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => string.IsNullOrWhiteSpace(info.Key) || c.Key.Contains(info.Key));
            if (string.IsNullOrWhiteSpace(info.GroupId) == false)
            {
                items = items.Where(c => c.GroupId == info.GroupId);
            }
            switch (info.Order)
            {
                case SForwardFlowOrder.Sendt:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case SForwardFlowOrder.DiffSendt:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffSendtBytes);
                    else
                        items = items.OrderBy(x => x.DiffSendtBytes);
                    break;
                case SForwardFlowOrder.Receive:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                case SForwardFlowOrder.DiffRecive:
                    if (info.OrderType == SForwardFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffReceiveBytes);
                    else
                        items = items.OrderBy(x => x.DiffReceiveBytes);
                    break;
                default:
                    break;
            }
            SForwardFlowResponseInfo resp = new SForwardFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class SForwardFlowItemInfo : FlowItemInfo
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

    public sealed partial class SForwardFlowRequestInfo
    {
        public string Key { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public SForwardFlowOrder Order { get; set; }
        public SForwardFlowOrderType OrderType { get; set; }
    }

    public enum SForwardFlowOrder : byte
    {
        Sendt = 1,
        DiffSendt = 2,
        Receive = 3,
        DiffRecive = 4
    }
    public enum SForwardFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class SForwardFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<SForwardFlowItemInfo> Data { get; set; }
    }
}