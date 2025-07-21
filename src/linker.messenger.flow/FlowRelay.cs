using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.relay.server;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace linker.messenger.flow
{
    public sealed class RelayResolverFlow : RelayServerResolver
    {
        private readonly FlowRelay relayFlow;
        public RelayResolverFlow(FlowRelay relayFlow, RelayServerNodeTransfer relayServerNodeTransfer, ISerializer serializer) : base(relayServerNodeTransfer, serializer)
        {
            this.relayFlow = relayFlow;
        }

        public override void Add(string key, string from, string to, string groupid, long receiveBytes, long sendtBytes)
        {
            relayFlow.Add(key, from, to, groupid, receiveBytes, sendtBytes);
        }
        public override void Add(string key, long receiveBytes, long sendtBytes)
        {
            relayFlow.Add(key, receiveBytes, sendtBytes);
        }

        public override long GetReceive()
        {
            return relayFlow.ReceiveBytes;
        }
        public override long GetSent()
        {
            return relayFlow.SendtBytes;
        }

    }

    public sealed class FlowRelay : IFlow
    {
        public long ReceiveBytes { get; private set; }
        public long SendtBytes { get; private set; }
        public string FlowName => "Relay";
        public VersionManager Version { get; } = new VersionManager();

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        private ConcurrentDictionary<string, RelayFlowItemInfo> flows = new ConcurrentDictionary<string, RelayFlowItemInfo>();

        public FlowRelay()
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

        public string GetItems() => flows.ToJson();
        public void SetItems(string json) { flows = json.DeJson<ConcurrentDictionary<string, RelayFlowItemInfo>>(); }
        public void SetBytes(long receiveBytes, long sendtBytes) { ReceiveBytes = receiveBytes; SendtBytes = sendtBytes; }
        public void Clear() { ReceiveBytes = 0; SendtBytes = 0; flows.Clear(); }

        public void Add(string key, string from, string to, string groupid, long receiveBytes, long sendtBytess)
        {
            if (flows.TryGetValue(key, out RelayFlowItemInfo messengerFlowItemInfo) == false)
            {
                messengerFlowItemInfo = new RelayFlowItemInfo();
                flows.TryAdd(key, messengerFlowItemInfo);
            }
            messengerFlowItemInfo.FromName = from;
            messengerFlowItemInfo.ToName = to;
            messengerFlowItemInfo.GroupId = groupid;

            ReceiveBytes += receiveBytes;
            messengerFlowItemInfo.ReceiveBytes += receiveBytes;
            SendtBytes += sendtBytess;
            messengerFlowItemInfo.SendtBytes += sendtBytess;
            Version.Increment();
        }

        public void Add(string key, long receiveBytes, long sendtBytess)
        {
            if (flows.TryGetValue(key, out RelayFlowItemInfo messengerFlowItemInfo))
            {
                ReceiveBytes += receiveBytes;
                messengerFlowItemInfo.ReceiveBytes += receiveBytes;
                SendtBytes += sendtBytess;
                messengerFlowItemInfo.SendtBytes += sendtBytess;
                Version.Increment();
            }

        }

        public RelayFlowResponseInfo GetFlows(RelayFlowRequestInfo info)
        {
            var items = flows.Values.Where(c => string.IsNullOrWhiteSpace(info.Key) || c.FromName.Contains(info.Key) || c.ToName.Contains(info.Key));
            if (string.IsNullOrWhiteSpace(info.GroupId) == false)
            {
                items = items.Where(c => c.GroupId == info.GroupId);
            }

            switch (info.Order)
            {
                case RelayFlowOrder.Sendt:
                    if (info.OrderType == RelayFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.SendtBytes);
                    else
                        items = items.OrderBy(x => x.SendtBytes);
                    break;
                case RelayFlowOrder.DiffSendt:
                    if (info.OrderType == RelayFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffSendtBytes);
                    else
                        items = items.OrderBy(x => x.DiffSendtBytes);
                    break;
                case RelayFlowOrder.Receive:
                    if (info.OrderType == RelayFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.ReceiveBytes);
                    else
                        items = items.OrderBy(x => x.ReceiveBytes);
                    break;
                case RelayFlowOrder.DiffRecive:
                    if (info.OrderType == RelayFlowOrderType.Desc)
                        items = items.OrderByDescending(x => x.DiffReceiveBytes);
                    else
                        items = items.OrderBy(x => x.DiffReceiveBytes);
                    break;
                default:
                    break;
            }
            RelayFlowResponseInfo resp = new RelayFlowResponseInfo
            {
                Page = info.Page,
                PageSize = info.PageSize,
                Count = flows.Count,
                Data = items.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList()
            };

            return resp;
        }
    }

    public sealed partial class RelayFlowItemInfo : FlowItemInfo
    {
        public long DiffReceiveBytes { get; set; }
        public long DiffSendtBytes { get; set; }
        public string FromName { get; set; }
        public string ToName { get; set; }

        public string GroupId { get; set; }

        [JsonIgnore]
        public long OldReceiveBytes { get; set; }
        [JsonIgnore]
        public long OldSendtBytes { get; set; }
    }

    public sealed partial class RelayFlowRequestInfo
    {
        public string Key { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public RelayFlowOrder Order { get; set; }
        public RelayFlowOrderType OrderType { get; set; }
    }

    public enum RelayFlowOrder : byte
    {
        Sendt = 1,
        DiffSendt = 2,
        Receive = 3,
        DiffRecive = 4
    }
    public enum RelayFlowOrderType : byte
    {
        Desc = 0,
        Asc = 1,
    }

    public sealed partial class RelayFlowResponseInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<RelayFlowItemInfo> Data { get; set; }
    }
}